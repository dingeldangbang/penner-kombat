/**
 * app.js — App-Shell für alle Seiten der Penner-Kombat-Web-App
 *
 * Kümmert sich um alles, was eine installierbare App vom „losen HTML" trennt:
 *   • Service-Worker registrieren (Offline-Betrieb)
 *   • Update-Hinweis, wenn eine neue Version im Hintergrund bereitliegt
 *   • „App installieren"-Knopf (beforeinstallprompt)
 *   • WebGL-Check mit verständlicher Fehlermeldung statt schwarzem Bild
 *   • Versionsanzeige
 *
 * Wird als klassisches Skript eingebunden (kein Modul), damit es auch dann
 * läuft, wenn ein Modul-Import scheitert.
 */
(function () {
  'use strict';

  var APP_VERSION = '1.1.0';
  window.PK_APP = { version: APP_VERSION };

  // --- Service Worker -------------------------------------------------------
  function registerSW() {
    if (!('serviceWorker' in navigator)) return;
    if (location.protocol === 'file:') return;   // ohne Server kein SW
    navigator.serviceWorker.register('sw.js').then(function (reg) {
      reg.addEventListener('updatefound', function () {
        var sw = reg.installing;
        if (!sw) return;
        sw.addEventListener('statechange', function () {
          if (sw.state === 'installed' && navigator.serviceWorker.controller) {
            showToast('Neue Version geladen — <button id="pk-reload">jetzt neu starten</button>', function (el) {
              var btn = el.querySelector('#pk-reload');
              if (btn) btn.addEventListener('click', function () {
                sw.postMessage('skip-waiting');
                location.reload();
              });
            });
          }
        });
      });
    }).catch(function () { /* SW ist optional */ });
  }

  // --- Installation ---------------------------------------------------------
  var deferredPrompt = null;
  window.addEventListener('beforeinstallprompt', function (e) {
    e.preventDefault();
    deferredPrompt = e;
    var btn = document.getElementById('btn-install');
    if (btn) {
      btn.hidden = false;
      btn.addEventListener('click', function () {
        btn.hidden = true;
        deferredPrompt.prompt();
        deferredPrompt.userChoice.finally(function () { deferredPrompt = null; });
      }, { once: true });
    } else {
      showToast('📲 Diese Seite lässt sich als App installieren — Browsermenü → „Installieren".');
    }
  });
  window.addEventListener('appinstalled', function () {
    var btn = document.getElementById('btn-install');
    if (btn) btn.hidden = true;
    showToast('✅ Penner Kombat ist installiert — läuft ab jetzt auch offline.');
  });

  // --- WebGL-Check ----------------------------------------------------------
  window.PK_APP.checkWebGL = function (canvas) {
    try {
      var c = canvas || document.createElement('canvas');
      var gl = c.getContext('webgl2') || c.getContext('webgl') || c.getContext('experimental-webgl');
      return !!gl;
    } catch (e) { return false; }
  };

  window.PK_APP.fatal = function (title, detail) {
    var box = document.createElement('div');
    box.setAttribute('role', 'alert');
    box.style.cssText = 'position:fixed;inset:0;z-index:9999;display:grid;place-items:center;' +
      'background:rgba(10,11,20,.95);color:#e8e9f2;font-family:system-ui,sans-serif;padding:24px;text-align:center';
    box.innerHTML = '<div style="max-width:460px"><h2 style="color:#FFD700;font-size:19px">' + title +
      '</h2><p style="color:#9ba0bb;line-height:1.6;font-size:14px">' + detail + '</p></div>';
    document.body.appendChild(box);
  };

  // --- Kleine Hinweiszeile --------------------------------------------------
  function showToast(html, onReady) {
    var el = document.createElement('div');
    el.className = 'pk-toast';
    el.innerHTML = html;
    el.style.cssText = 'position:fixed;left:50%;bottom:16px;transform:translateX(-50%);z-index:9998;' +
      'background:#21243a;border:1px solid rgba(255,255,255,.14);color:#e8e9f2;padding:9px 14px;' +
      'border-radius:10px;font:13px/1.4 system-ui,sans-serif;box-shadow:0 8px 24px rgba(0,0,0,.45);max-width:92vw';
    var style = el.querySelector('button');
    document.body.appendChild(el);
    el.querySelectorAll('button').forEach(function (b) {
      b.style.cssText = 'margin-left:8px;background:#8B0000;color:#fff;border:0;border-radius:6px;padding:4px 10px;font:inherit;font-weight:700;cursor:pointer';
    });
    if (onReady) onReady(el);
    setTimeout(function () { el.style.transition = 'opacity .4s'; el.style.opacity = '0'; setTimeout(function () { el.remove(); }, 400); }, 9000);
    return el;
  }
  window.PK_APP.toast = showToast;

  // --- Versionsanzeige ------------------------------------------------------
  document.addEventListener('DOMContentLoaded', function () {
    var tags = document.querySelectorAll('[data-app-version]');
    for (var i = 0; i < tags.length; i++) tags[i].textContent = 'v' + APP_VERSION;
  });

  if (document.readyState === 'complete') registerSW();
  else window.addEventListener('load', registerSW);
})();
