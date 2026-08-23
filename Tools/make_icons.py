#!/usr/bin/env python3
"""Erzeugt die Android-Launcher-Icons aus icon.svg -- ohne Fremdbibliotheken.

Warum nicht einfach ImageMagick/rsvg?
------------------------------------
Auf vielen CI-Runnern (und in dieser Umgebung) fehlt der SVG-Delegate von
ImageMagick, `convert icon.svg icon.png` bricht dann ab. Damit der Build
nirgends an einer optionalen Systemabhaengigkeit haengt, zeichnet dieses
Skript die Icon-Motive direkt nach und schreibt die PNGs mit `zlib` selbst.

Erzeugt:
    android/icons/icon_192.png                  Launcher-Icon (klassisch)
    android/icons/icon_adaptive_fg_432.png      Adaptive Icon, Vordergrund
    android/icons/icon_adaptive_bg_432.png      Adaptive Icon, Hintergrund

Aufruf:
    python3 Tools/make_icons.py

Die Motive entsprechen icon.svg: dunkler Radialverlauf, roter Blutstropfen,
weisser Bogen und das "PK"-Band.

Adaptive Icons: Android beschneidet den Vordergrund auf einen Kreis von ca.
66 % Kantenlaenge. Der Vordergrund wird deshalb auf 66 % herunterskaliert in
die Mitte gesetzt, damit nichts abgeschnitten wird.
"""

from __future__ import annotations

import math
import os
import struct
import zlib

SS = 4  # Supersampling-Faktor fuer weiche Kanten


# --------------------------------------------------------------------------
# PNG-Ausgabe
# --------------------------------------------------------------------------

def write_png(path: str, width: int, height: int, rgba: bytearray) -> None:
    """Schreibt RGBA8-Rohdaten als PNG (Filter 0 pro Zeile)."""
    raw = bytearray()
    stride = width * 4
    for y in range(height):
        raw.append(0)
        raw += rgba[y * stride:(y + 1) * stride]

    def chunk(tag: bytes, data: bytes) -> bytes:
        return (struct.pack(">I", len(data)) + tag + data
                + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF))

    png = b"\x89PNG\r\n\x1a\n"
    png += chunk(b"IHDR", struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0))
    png += chunk(b"IDAT", zlib.compress(bytes(raw), 9))
    png += chunk(b"IEND", b"")

    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "wb") as fh:
        fh.write(png)


# --------------------------------------------------------------------------
# Zeichen-Primitive (Koordinaten im 512er-Raum von icon.svg)
# --------------------------------------------------------------------------

def lerp(a, b, t):
    return a + (b - a) * t


def mix(c1, c2, t):
    return tuple(lerp(c1[i], c2[i], t) for i in range(3))


def rounded_rect_hit(x, y, w, h, r):
    if x < 0 or y < 0 or x >= w or y >= h:
        return False
    cx = min(max(x, r), w - r)
    cy = min(max(y, r), h - r)
    return (x - cx) ** 2 + (y - cy) ** 2 <= r * r


def point_in_poly(x, y, poly):
    inside = False
    n = len(poly)
    j = n - 1
    for i in range(n):
        xi, yi = poly[i]
        xj, yj = poly[j]
        if (yi > y) != (yj > y):
            xint = (xj - xi) * (y - yi) / (yj - yi) + xi
            if x < xint:
                inside = not inside
        j = i
    return inside


def dist_to_segment(px, py, ax, ay, bx, by):
    dx, dy = bx - ax, by - ay
    if dx == 0 and dy == 0:
        return math.hypot(px - ax, py - ay)
    t = max(0.0, min(1.0, ((px - ax) * dx + (py - ay) * dy) / (dx * dx + dy * dy)))
    return math.hypot(px - (ax + t * dx), py - (ay + t * dy))


def near_polyline(x, y, pts, half_width):
    for i in range(len(pts) - 1):
        if dist_to_segment(x, y, pts[i][0], pts[i][1],
                           pts[i + 1][0], pts[i + 1][1]) <= half_width:
            return True
    return False


# Blutstropfen: der SVG-Pfad als Polygon nachgebildet.
def blood_polygon():
    pts = [(258.0, 48.0)]
    # linke Flanke nach unten
    for i in range(1, 21):
        t = i / 20.0
        x = 258 - 92 * math.sin(t * math.pi * 0.5) ** 1.35
        y = 48 + 183 * t
        pts.append((x, y))
    # untere Rundung (Halbkreis um (258, 231) mit r=92, leicht oval)
    for i in range(21):
        a = math.pi + i / 20.0 * math.pi
        pts.append((258 + 92 * math.cos(a) * -1, 231 + 115 * math.sin(a) * -1))
    # rechte Flanke zurueck nach oben
    for i in range(20, 0, -1):
        t = i / 20.0
        x = 258 + 92 * math.sin(t * math.pi * 0.5) ** 1.35
        y = 48 + 183 * t
        pts.append((x, y))
    return pts


BLOOD = blood_polygon()

# Weisser Bogen unter dem Tropfen.
ARC = [(165 + (351 - 165) * i / 24.0,
        256 + 51 * math.sin(math.pi * (i / 24.0)) * 0.86)
       for i in range(25)]

# "PK" als Block-Buchstaben (Polygone), passend ins Band bei y 362..420.
P_STEM = [(196, 372), (208, 372), (208, 412), (196, 412)]
P_BOWL_OUT = [(208, 372), (232, 372), (238, 378), (238, 390),
              (232, 396), (208, 396)]
P_BOWL_IN = [(208, 379), (229, 379), (231, 382), (231, 386), (229, 389),
             (208, 389)]
K_STEM = [(258, 372), (270, 372), (270, 412), (258, 412)]
K_UP = [(270, 388), (288, 372), (300, 372), (277, 392), (270, 392)]
K_DOWN = [(270, 392), (277, 392), (301, 412), (288, 412), (270, 396)]

BG_IN = (0x28, 0x14, 0x24)
BG_OUT = (0x08, 0x06, 0x0b)
BLOOD_A = (0xff, 0x33, 0x33)
BLOOD_B = (0x65, 0x00, 0x00)
BAND = (0x14, 0x10, 0x18)
BAND_EDGE = (0xb7, 0x2a, 0x2a)
WHITE = (0xff, 0xff, 0xff)


def shade(x, y, with_bg: bool):
    """Farbe + Deckkraft an Punkt (x, y) im 512er-Raum. None = transparent."""
    if with_bg:
        if not rounded_rect_hit(x, y, 512, 512, 96):
            return None
        d = math.hypot(x - 256, y - 230) / (0.70 * 512)
        col = mix(BG_IN, BG_OUT, min(1.0, d))
    else:
        col = None

    # Band mit Rand
    if 110 <= x <= 402 and 357 <= y <= 425:
        inner = 115 <= x <= 397 and 362 <= y <= 420
        edge = not inner
        col = BAND_EDGE if edge else BAND

    # Blutstropfen
    if point_in_poly(x, y, BLOOD):
        t = ((x - 166) / 184.0 + (y - 48) / 298.0) / 2.0
        col = mix(BLOOD_A, BLOOD_B, max(0.0, min(1.0, t)))

    # weisser Bogen
    if near_polyline(x, y, ARC, 9):
        col = WHITE

    # "PK"
    if 355 <= y <= 425:
        in_p = (point_in_poly(x, y, P_STEM) or point_in_poly(x, y, P_BOWL_OUT)) \
            and not point_in_poly(x, y, P_BOWL_IN)
        in_k = (point_in_poly(x, y, K_STEM) or point_in_poly(x, y, K_UP)
                or point_in_poly(x, y, K_DOWN))
        if in_p or in_k:
            col = WHITE

    return col


def render(size: int, with_bg: bool, content_scale: float = 1.0) -> bytearray:
    """Rendert das Motiv mit Supersampling in ein RGBA-Puffer."""
    buf = bytearray(size * size * 4)
    n = size * SS
    inv = 512.0 / n
    off = (1.0 - content_scale) * 0.5 * 512.0

    for py in range(size):
        for px in range(size):
            r = g = b = a = 0.0
            for sy in range(SS):
                for sx in range(SS):
                    fx = ((px * SS + sx) + 0.5) * inv
                    fy = ((py * SS + sy) + 0.5) * inv
                    # Inhalt skalieren (fuer die Safe-Zone adaptiver Icons)
                    ux = (fx - off) / content_scale
                    uy = (fy - off) / content_scale
                    c = shade(ux, uy, with_bg)
                    if c is not None:
                        r += c[0]
                        g += c[1]
                        b += c[2]
                        a += 255.0
            k = SS * SS
            i = (py * size + px) * 4
            if a > 0:
                # Farben nur ueber die gedeckten Subpixel mitteln
                cov = a / 255.0
                buf[i] = int(r / cov)
                buf[i + 1] = int(g / cov)
                buf[i + 2] = int(b / cov)
                buf[i + 3] = int(a / k)
            else:
                buf[i:i + 4] = b"\x00\x00\x00\x00"
    return buf


def render_bg_only(size: int) -> bytearray:
    """Volle Flaeche fuer den adaptiven Hintergrund (kein abgerundeter Rand --
    die Maske uebernimmt Android)."""
    buf = bytearray(size * size * 4)
    for py in range(size):
        for px in range(size):
            x = (px + 0.5) * 512.0 / size
            y = (py + 0.5) * 512.0 / size
            d = math.hypot(x - 256, y - 230) / (0.70 * 512)
            c = mix(BG_IN, BG_OUT, min(1.0, d))
            i = (py * size + px) * 4
            buf[i] = int(c[0])
            buf[i + 1] = int(c[1])
            buf[i + 2] = int(c[2])
            buf[i + 3] = 255
    return buf


def main() -> int:
    root = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
    out = os.path.join(root, "android", "icons")

    print("icon_192.png ...")
    write_png(os.path.join(out, "icon_192.png"), 192, 192, render(192, True))

    # Adaptive Icons: Vordergrund auf 66 % (Safe Zone), Hintergrund vollflaechig.
    print("icon_adaptive_fg_432.png ...")
    write_png(os.path.join(out, "icon_adaptive_fg_432.png"), 432, 432,
              render(432, False, content_scale=0.66))

    print("icon_adaptive_bg_432.png ...")
    write_png(os.path.join(out, "icon_adaptive_bg_432.png"), 432, 432,
              render_bg_only(432))

    print("fertig -> android/icons/")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
