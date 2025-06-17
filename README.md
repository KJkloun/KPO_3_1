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
- .NET 8 SDK (для локальной разработки)

### Запуск системы

```bash
# Клонирование репозитория
git clone https://github.com/KJkloun/KPO_3_1.git
cd KPO_3

# Запуск всей системы в Docker
docker-compose up --build -d

# Проверка статуса контейнеров
docker-compose ps
```

### Доступ к сервисам
- **Frontend**: http://localhost:3000
- **API Gateway**: http://localhost:8080
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

## Мониторинг и отладка

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

### Подключение к SQL Server
```bash
# Через Docker
docker exec -it kpo_3-sqlserver-1 /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P YourStrong@Passw0rd

# Через SQL Server Management Studio
Server: localhost,1433
Login: sa
Password: YourStrong@Passw0rd
```

### Проверка таблиц
```sql
-- Базы данных
SELECT name FROM sys.databases WHERE name IN ('OrdersDb', 'PaymentsDb');

-- Таблицы Orders
USE OrdersDb;
SELECT * FROM Orders;
SELECT * FROM OutboxMessages;

-- Таблицы Payments  
USE PaymentsDb;
SELECT * FROM Accounts;
SELECT * FROM Transactions;
SELECT * FROM InboxMessages;
```

## Режимы работы

### Docker (Production)
- **База данных**: SQL Server
- **Сообщения**: RabbitMQ
- **Асинхронность**: Включена
- **Транзакции**: Полные гарантии

### Local Development
- **База данных**: InMemory
- **Сообщения**: Отключены
- **Асинхронность**: Отключена
- **Использование**: Для отладки

```bash
# Локальный запуск (для разработки)
cd src/Gateway/ApiGateway && dotnet run --urls=http://localhost:5000
cd src/Services/Orders/Orders.Api && ASPNETCORE_ENVIRONMENT=Development dotnet run --urls=http://localhost:5001
cd src/Services/Payments/Payments.Api && ASPNETCORE_ENVIRONMENT=Development dotnet run --urls=http://localhost:5002
```

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
│   │   └── Orders.Infrastructure/ # Данные, Outbox
│   └── Payments/               # Сервис платежей
│       ├── Payments.Api/       # REST API
│       ├── Payments.Application/ # Бизнес-логика (CQRS)
│       ├── Payments.Domain/    # Доменная модель
│       └── Payments.Infrastructure/ # Данные, Inbox
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

### Упрощения
- Account ID = User ID (единый идентификатор)
- Автоматическое создание баз данных
- Базовая обработка ошибок

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
