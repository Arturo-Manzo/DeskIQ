#!/bin/sh
set -eu

DESKIQ_FRONTEND_API_BASE_URL="${DESKIQ_FRONTEND_API_BASE_URL:-http://localhost:5000}"
export DESKIQ_FRONTEND_API_BASE_URL

envsubst '$DESKIQ_FRONTEND_API_BASE_URL' < /etc/deskiq/config.js.template > /usr/share/nginx/html/config.js

exec nginx -g 'daemon off;'
