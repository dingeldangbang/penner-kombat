#!/usr/bin/env python3
"""Erzeugt test_assets/metarig_test.glb — einen minimalen, aber vollwertigen
GLB-Testcharakter mit Skelett (Metarig) für die Pipeline-Verifikation.

Der Metarig folgt den Namenskonventionen, die DynamicRigger erwartet:
Root, Hips, Spine, Head, UpperArm_L/R, Forearm_L/R, Hand_L/R,
UpperLeg_L/R, LowerLeg_L/R  ->  Head, Hand_L, Hand_R und Root werden
vom Rigger als Knochen erkannt.

Das Skript braucht NUR die Python-Standardbibliothek.

  python3 Tools/make_metarig_glb.py [ausgabepfad]
"""
import json
import struct
import sys
from pathlib import Path

OUT = (
    Path(sys.argv[1])
    if len(sys.argv) > 1
    else Path(__file__).resolve().parent.parent / "test_assets" / "metarig_test.glb"
)

BONES = [
    # (name, parent_index_or_None, translation)
    ("Root", None, (0.0, 0.0, 0.0)),
    ("Hips", 0, (0.0, 0.9, 0.0)),
    ("Spine", 1, (0.0, 1.15, 0.0)),
    ("Head", 2, (0.0, 1.45, 0.0)),
    ("UpperArm_L", 2, (-0.35, 1.35, 0.0)),
    ("Forearm_L", 4, (-0.62, 1.35, 0.0)),
    ("Hand_L", 5, (-0.88, 1.35, 0.0)),
    ("UpperArm_R", 2, (0.35, 1.35, 0.0)),
    ("Forearm_R", 7, (0.62, 1.35, 0.0)),
    ("Hand_R", 8, (0.88, 1.35, 0.0)),
    ("UpperLeg_L", 1, (-0.16, 0.9, 0.0)),
    ("LowerLeg_L", 10, (-0.16, 0.48, 0.0)),
    ("UpperLeg_R", 1, (0.16, 0.9, 0.0)),
    ("LowerLeg_R", 12, (0.16, 0.48, 0.0)),
]

# Hüftknochen = Joint-Index 1 -> alle Gewichte hängen dort (Testcharakter).
HIPS_JOINT = 1

# Box um den Hüftknochen: Halb-Achsen
HX, HY, HZ = 0.4, 0.8, 0.2


def build_mesh_geometry():
    """24 Vertices (4 pro Seite, geteilte Normalen) + 36 Indizes, Box 0.8x1.6x0.4
    zentriert auf (0, 0.9, 0)."""
    base_y = 0.9
    faces = [
        # (normal, u_axis, v_axis)
        ((0, 0, 1), (1, 0, 0), (0, 1, 0)),
        ((0, 0, -1), (-1, 0, 0), (0, 1, 0)),
        ((1, 0, 0), (0, 0, -1), (0, 1, 0)),
        ((-1, 0, 0), (0, 0, 1), (0, 1, 0)),
        ((0, 1, 0), (1, 0, 0), (0, 0, -1)),
        ((0, -1, 0), (1, 0, 0), (0, 0, 1)),
    ]
    def half(axis_vec):
        if axis_vec[0]:
            return HX
        if axis_vec[1]:
            return HY
        return HZ

    positions, normals, indices = [], [], []
    base = 0
    for normal, u, v in faces:
        eu, ev = half(u), half(v)
        for su, sv in ((-1, -1), (1, -1), (1, 1), (-1, 1)):
            px = su * u[0] * eu + sv * v[0] * ev
            py = base_y + su * u[1] * eu + sv * v[1] * ev
            pz = su * u[2] * eu + sv * v[2] * ev
            positions.append((px, py, pz))
            normals.append(normal)
        for a, b, c in ((0, 1, 2), (0, 2, 3)):
            indices.append(base + a)
            indices.append(base + b)
            indices.append(base + c)
        base += 4
    return positions, normals, indices


def pack_bin():
    positions, normals, indices = build_mesh_geometry()
    padding = lambda blob: blob + b"\x00" * ((4 - len(blob) % 4) % 4)

    blobs = [
        padding(b"".join(struct.pack("<3f", *p) for p in positions)),
        padding(b"".join(struct.pack("<3f", *n) for n in normals)),
        padding(b"".join(struct.pack("<4B", HIPS_JOINT, 0, 0, 0) for _ in positions)),
        padding(b"".join(struct.pack("<4f", 1.0, 0.0, 0.0, 0.0) for _ in positions)),
        padding(b"".join(struct.pack("<H", i) for i in indices)),
        padding(
            b"".join(struct.pack("<16f", *([1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1])) for _ in BONES)
        ),
    ]

    offsets, total = [], 0
    for blob in blobs:
        offsets.append(total)
        total += len(blob)
    return b"".join(blobs), offsets, [len(b) for b in blobs], positions


def build_glb():
    binary, offsets, lengths, positions = pack_bin()
    num_verts = len(positions)

    nodes = [{"name": name, "translation": list(t)} for name, _p, t in BONES]
    for idx, (_name, parent, _t) in enumerate(BONES):
        if parent is not None:
            nodes[parent].setdefault("children", []).append(idx)
    nodes.append({"name": "Body", "mesh": 0, "skin": 0})

    xs = [p[0] for p in positions]
    ys = [p[1] for p in positions]
    zs = [p[2] for p in positions]

    accessors = [
        {"bufferView": 0, "componentType": 5126, "count": num_verts, "type": "VEC3",
         "min": [min(xs), min(ys), min(zs)], "max": [max(xs), max(ys), max(zs)]},
        {"bufferView": 1, "componentType": 5126, "count": num_verts, "type": "VEC3"},
        {"bufferView": 2, "componentType": 5121, "count": num_verts, "type": "VEC4"},
        {"bufferView": 3, "componentType": 5126, "count": num_verts, "type": "VEC4"},
        {"bufferView": 4, "componentType": 5123, "count": len(list(range(36))), "type": "SCALAR"},
        {"bufferView": 5, "componentType": 5126, "count": len(BONES), "type": "MAT4"},
    ]

    gltf = {
        "asset": {"version": "2.0", "generator": "penner-kombat metarig generator"},
        "scene": 0,
        "scenes": [{"name": "MetarigTest", "nodes": [0]}],
        "nodes": nodes,
        "skins": [{"name": "Metarig", "inverseBindMatrices": 5, "skeleton": 0,
                   "joints": list(range(len(BONES)))}],
        "meshes": [{"name": "Body", "primitives": [{
            "attributes": {"POSITION": 0, "NORMAL": 1, "JOINTS_0": 2, "WEIGHTS_0": 3},
            "indices": 4, "material": 0, "mode": 4}]}],
        "materials": [{"name": "TestSkin", "pbrMetallicRoughness": {
            "baseColorFactor": [0.8, 0.22, 0.18, 1.0],
            "roughnessFactor": 0.55, "metallicFactor": 0.0}}],
        "bufferViews": [
            {"buffer": 0, "byteOffset": offsets[0], "byteLength": lengths[0], "target": 34962},
            {"buffer": 0, "byteOffset": offsets[1], "byteLength": lengths[1], "target": 34962},
            {"buffer": 0, "byteOffset": offsets[2], "byteLength": lengths[2], "target": 34962},
            {"buffer": 0, "byteOffset": offsets[3], "byteLength": lengths[3], "target": 34962},
            {"buffer": 0, "byteOffset": offsets[4], "byteLength": lengths[4], "target": 34963},
            {"buffer": 0, "byteOffset": offsets[5], "byteLength": lengths[5]},
        ],
        "accessors": accessors,
        "buffers": [{"byteLength": len(binary)}],
    }

    json_blob = json.dumps(gltf, separators=(",", ":")).encode("utf-8")
    json_blob += b" " * ((4 - len(json_blob) % 4) % 4)
    bin_blob = binary + b"\x00" * ((4 - len(binary) % 4) % 4)

    total = 12 + 8 + len(json_blob) + 8 + len(bin_blob)
    header = struct.pack("<4sII", b"glTF", 2, total)
    chunk0 = struct.pack("<I4s", len(json_blob), b"JSON") + json_blob
    chunk1 = struct.pack("<I4s", len(bin_blob), b"BIN\x00") + bin_blob
    return header + chunk0 + chunk1


def main():
    OUT.parent.mkdir(parents=True, exist_ok=True)
    data = build_glb()
    OUT.write_bytes(data)
    print(f"OK: {OUT} ({len(data)} Bytes, {len(BONES)} Knochen)")


if __name__ == "__main__":
    main()
