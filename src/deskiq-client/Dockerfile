FROM node:20-alpine AS build
WORKDIR /app

# Install git for cloning ui-design-system
RUN apk add --no-cache git

COPY src/deskiq-client/package.json ./
COPY src/deskiq-client/package-lock.json ./
RUN npm ci --ignore-scripts

# Manually clone and build ui-design-system to bypass Windows-specific prepare script
RUN git clone https://github.com/Arturo-Manzo/ui-design-system.git /tmp/ui-design-system && \
    cd /tmp/ui-design-system && \
    npm ci --ignore-scripts && \
    npx ng build ui-design-system && \
    mkdir -p /app/node_modules/ui-design-system && \
    cp -r dist/ui-design-system/* /app/node_modules/ui-design-system/ && \
    rm -rf /tmp/ui-design-system

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
