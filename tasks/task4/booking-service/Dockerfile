FROM golang:1.21-alpine AS builder

# Устанавливаем рабочую директорию внутри контейнера сборщика
WORKDIR /app

# Копируем весь остальной исходный код
COPY booking-service/main.go .

# Компилируем приложение в исполняемый бинарный файл с именем "booking-service"
RUN go build -o booking-service main.go

# --- ЭТАП 2: ЗАПУСК (RUN STAGE) ---
# Используем минимальный образ alpine:latest для запуска готового бинарника
FROM alpine:latest

# Устанавливаем утилиту curl, которая нужна для выполнения healthcheck-скрипта внутри контейнера
RUN apk add --no-cache curl

# Устанавливаем рабочую директорию в итоговом образе
WORKDIR /app

# Копируем ТОЛЬКО готовый бинарник из этапа сборки (builder)
COPY --from=builder /app/booking-service .

# Копируем скрипт проверки DNS, который лежит в той же директории, что и Dockerfile
COPY check-dns.sh check-status.sh ./

# Делаем скрипт исполняемым
RUN chmod +x check-dns.sh check-status.sh

# Информируем Docker о том, что сервис слушает порт 8080
EXPOSE 8080

# Команда запуска приложения (Exec Form, предпочтительный формат)
CMD ["./booking-service"]