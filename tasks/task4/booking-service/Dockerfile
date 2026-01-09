# Используем официальный минимальный образ Go
FROM golang:1.21-alpine

# TODO: Добавить необходимые пакеты (например, curl)
# Ожидаемый код:
RUN apk add --no-cache curl

# Устанавливаем рабочую директорию
WORKDIR /app

# Копируем Go-файл и скрипт проверки DNS
COPY booking-service/main.go .
COPY check-dns.sh check-status.sh ./

# TODO: Сделать скрипт исполняемым
# Ожидаемый код:
RUN chmod +x check-dns.sh

# TODO: Собрать бинарник из main.go
# Ожидаемый код:
RUN go build -o booking-service main.go

# Открываем порт, на котором слушает сервис (8080)
EXPOSE 8080

# TODO: Задать команду запуска
# Ожидаемый код:
CMD ["./booking-service"]
