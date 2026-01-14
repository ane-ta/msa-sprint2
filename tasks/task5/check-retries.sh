#!/bin/bash

set -e

echo "▶️ Checking retries on 5xx flaky errors..."

# Посылаем 100 запросов
for i in {1..100}
do
    curl -s http://localhost:9090/ping-flaky
done