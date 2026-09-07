#!/usr/bin/env bash
# Penner Kombat — Relay-Server lokal/auf dem VPS deployen (Docker).
#
#   ./Tools/server_deploy.sh              # bauen + starten (Relay)
#   ./Tools/server_deploy.sh tls          # zusätzlich Caddy+TLS (PK_DOMAIN setzen!)
#   ./Tools/server_deploy.sh status       # Health-Check + Status anzeigen
#   ./Tools/server_deploy.sh logs         # Logs folgen
#   ./Tools/server_deploy.sh stop         # Stack stoppen
#
# Voraussetzungen: Docker + Docker Compose v2.
set -e
cd "$(dirname "$0")/.."

CMD="${1:-up}"

case "$CMD" in
  up)
    docker compose up -d --build relay
    echo "→ Relay gestartet. Health-Check:"
    sleep 2
    curl -fsS http://127.0.0.1:${PORT:-5000}/health || echo "(Health-Check noch nicht bereit — 'status' erneut aufrufen)"
    echo
    echo "Client-URL: ws://<SERVER-IP>:${PORT:-5000}/kombat   (Android-Produktion: wss:// via tls!)"
    ;;
  tls)
    : "${PK_DOMAIN:?Bitte PK_DOMAIN setzen, z. B. PK_DOMAIN=kombat.example.de ./Tools/server_deploy.sh tls}"
    docker compose --profile tls up -d --build
    echo "→ TLS aktiv. Client-URL: wss://${PK_DOMAIN}/kombat (Firewall: 80/443 öffnen, DNS-Record setzen)"
    ;;
  status)
    curl -fsS http://127.0.0.1:${PORT:-5000}/api/status
    echo
    ;;
  logs)
    docker compose logs -f relay
    ;;
  stop)
    docker compose --profile tls down
    ;;
  *)
    echo "Unbekannt: $CMD (up | tls | status | logs | stop)"
    exit 1
    ;;
esac
