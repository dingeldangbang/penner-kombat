# Penner Kombat Relay — schlankes Container-Image
# Bau:  docker build -t penner-kombat-relay ./server
# Lauf: docker run -p 5000:5000 --restart unless-stopped penner-kombat-relay
FROM node:20-alpine

WORKDIR /app

# Nur Produktions-Abhängigkeiten (ws)
COPY package.json package-lock.json* ./
RUN npm install --omit=dev --no-audit --no-fund

COPY src ./src

ENV NODE_ENV=production \
    PORT=5000 \
    HOST=0.0.0.0 \
    MAX_PLAYERS=4 \
    MATCHMAKING_ENABLED=true

EXPOSE 5000

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD wget -qO- http://127.0.0.1:5000/health || exit 1

CMD ["node", "src/relay.js"]
