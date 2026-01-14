#!/bin/bash

set -e

echo "▶️ Проверка Feature Flag (header X-Feature-Enabled: true)..."

# Отправляем запрос с заголовком, чтобы маршрутизировать трафик на `v2`
curl -H "X-Feature-Enabled: true" http://localhost:9090/feature

echo "▶️ Проверка Feature Flag (header X-Feature-Enabled: false)..."

# Отправляем запрос с заголовком, чтобы маршрутизировать трафик на `v2`
curl -H "X-Feature-Enabled: false" http://localhost:9090/feature

echo "▶️ Проверка no Feature Flag (no header X-Feature-Enabled)..."

# Отправляем запрос с заголовком, чтобы маршрутизировать трафик на `v2`
curl http://localhost:9090/feature
