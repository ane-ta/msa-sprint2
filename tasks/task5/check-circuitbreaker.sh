#!/bin/bash

set -e

echo "▶️ Checking Circuit breaker..."

# Посылаем 100 запросов
for i in {1..20}
do
    curl -s http://localhost:9090/ping-error
done