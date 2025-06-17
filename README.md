# E-commerce Микросервисная Архитектура

Микросервисная система электронной коммерции на .NET 8 с асинхронной обработкой платежей.

## Архитектура

### Микросервисы
- **API Gateway** (YARP) - Единая точка входа, маршрутизация запросов
- **Orders Service** - Управление заказами, проверка баланса
- **Payments Service** - Обработка платежей, управление счетами
- **Frontend** - Веб-интерфейс для тестирования API

### Технологический стек
- **.NET 8** - Основная платформа
- **Clean Architecture** - Архитектурный паттерн
- **CQRS + MediatR** - Разделение команд и запросов
- **Entity Framework Core** - ORM для работы с БД
- **SQL Server** - Основная база данных
- **RabbitMQ** - Асинхронная передача сообщений
- **Docker** - Контейнеризация
- **YARP** - Reverse Proxy

### Паттерны
- **Transactional Outbox** - Гарантированная доставка сообщений
- **Transactional Inbox** - Дедупликация входящих сообщений
- **SAGA** - Управление распределенными транзакциями
- **Domain Events** - Событийно-ориентированная архитектура

## Быстрый старт

### Требования
- Docker Desktop

### Запуск системы

```bash
# Клонирование репозитория
git clone https://github.com/KJkloun/KPO_3_1.git
cd KPO_3_1

# Запуск всей системы в Docker
docker-compose up --build -d

# Проверка статуса контейнеров
docker-compose ps
```

### Доступ к сервисам
- **Frontend**: http://localhost:3000
- **API Gateway**: http://localhost:8080
- **Swagger UI**: http://localhost:8080/swagger
- **RabbitMQ Management**: http://localhost:15672 (admin/admin)
- **SQL Server**: localhost:1433 (sa/YourStrong@Passw0rd)

## Работа с системой

### Создание аккаунта и заказа
1. Откройте http://localhost:3000
2. Создайте аккаунт пользователя
3. Пополните баланс на нужную сумму
4. Создайте заказ (система проверит баланс)
5. Деньги спишутся асинхронно через RabbitMQ

### API Endpoints

#### Payments Service
```
POST /api/payments/accounts           # Создание аккаунта
POST /api/payments/accounts/{id}/top-up  # Пополнение баланса
GET  /api/payments/accounts/{id}/balance # Проверка баланса
```

#### Orders Service
```
POST /api/orders                      # Создание заказа
GET  /api/orders?userId={id}          # Список заказов пользователя
```

## Тестирование

### Запуск тестов
```bash
# Запуск всех тестов
dotnet test

# Запуск тестов конкретного сервиса
dotnet test src/Services/Orders/Orders.Tests
dotnet test src/Services/Payments/Payments.Tests
```

### Покрытие кода
- **Orders Service**: >70% покрытия
- **Payments Service**: >70% покрытия
- **Интеграционные тесты**: Проверка взаимодействия сервисов

## Мониторинг

### Просмотр логов
```bash
# Логи всех сервисов
docker-compose logs -f

# Логи конкретного сервиса
docker-compose logs -f orders-api
docker-compose logs -f payments-api
docker-compose logs -f api-gateway
```

### Мониторинг RabbitMQ
1. Откройте http://localhost:15672
2. Войдите: admin/admin
3. Вкладка **Queues** - очереди сообщений
4. Вкладка **Exchanges** - маршрутизация сообщений

## Структура проекта

```
src/
├── Gateway/
│   └── ApiGateway/              # YARP Reverse Proxy
├── Services/
│   ├── Orders/                  # Сервис заказов
│   │   ├── Orders.Api/         # REST API
│   │   ├── Orders.Application/ # Бизнес-логика (CQRS)
│   │   ├── Orders.Domain/      # Доменная модель
│   │   ├── Orders.Infrastructure/ # Данные, Outbox
│   │   └── Orders.Tests/       # Модульные тесты
│   └── Payments/               # Сервис платежей
│       ├── Payments.Api/       # REST API
│       ├── Payments.Application/ # Бизнес-логика (CQRS)
│       ├── Payments.Domain/    # Доменная модель
│       ├── Payments.Infrastructure/ # Данные, Inbox
│       └── Payments.Tests/     # Модульные тесты
└── Shared/                     # Общие компоненты
    ├── Shared.Contracts/       # Сообщения и контракты
    └── Shared.Infrastructure/  # RabbitMQ, Outbox/Inbox
frontend/                       # Веб-интерфейс
docker-compose.yml              # Оркестрация контейнеров
```

## Особенности реализации

### Проверка баланса
- Синхронная проверка через HTTP при создании заказа
- Асинхронное списание через RabbitMQ после создания заказа
- Идемпотентность операций

### Гарантии
- **Exactly-once processing** - Outbox/Inbox паттерны
- **Консистентность** - Транзакционные границы
- **Отказоустойчивость** - Retry механизмы RabbitMQ

## Решение проблем

### Контейнеры не запускаются
```bash
# Очистка и перезапуск
docker-compose down -v
docker-compose up --build -d
```

### Ошибки подключения к БД
```bash
# Проверка статуса SQL Server
docker-compose logs sqlserver

# Пересоздание volumes
docker-compose down -v
docker volume prune -f
```

### RabbitMQ недоступен
```bash
# Перезапуск RabbitMQ
docker-compose restart rabbitmq

# Проверка портов
netstat -an | grep 5672
netstat -an | grep 15672
```

### Порты заняты
```bash
# Освобождение портов
sudo lsof -ti:3000,8080,5672,1433,15672 | xargs kill -9
``` 