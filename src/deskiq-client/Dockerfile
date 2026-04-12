FROM node:20-alpine AS build
WORKDIR /app

COPY src/deskiq-client/package*.json ./
RUN npm ci

COPY src/deskiq-client/ ./
RUN npm run build

FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html
RUN apk add --no-cache gettext
COPY src/deskiq-client/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/deskiq-client/browser/ ./
COPY src/deskiq-client/docker/config.js.template /etc/deskiq/config.js.template
COPY src/deskiq-client/docker/entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh
EXPOSE 80
ENTRYPOINT ["/entrypoint.sh"]
