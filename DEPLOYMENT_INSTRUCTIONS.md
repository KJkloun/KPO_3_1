# 🚀 Развертывание E-commerce Микросервисов с Фронтендом

## 📋 Обзор системы

Полная микросервисная система с веб-интерфейсом:

### Компоненты:
- **Frontend** (Nginx) - http://localhost:3000
- **API Gateway** (YARP) - http://localhost:8080  
- **Orders Service** - Сервис заказов
- **Payments Service** - Сервис платежей
- **SQL Server** - База данных
- **RabbitMQ** - Очередь сообщений

## 🏗️ Архитектура

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Frontend      │    │   API Gateway   │    │   Orders API    │
│   (Nginx)       │───▶│   (YARP)        │───▶│   (Orders DB)   │
│   Port: 3000    │    │   Port: 8080    │    │                 │
└─────────────────┘    └─────────────────┘    └─────────────────┘
                                │                        │
                                │                        ▼
                                │               ┌─────────────────┐
                                │               │   RabbitMQ      │
                                │               │   Port: 5672    │
                                │               │   UI: 15672     │
                                │               └─────────────────┘
                                │                        ▲
                                │                        │
                                ▼               ┌─────────────────┐
                       ┌─────────────────┐     │   Payments API  │
                       │   SQL Server    │◀────│   (Payments DB) │
                       │   Port: 1433    │     │                 │
                       └─────────────────┘     └─────────────────┘
```

## 🚀 Быстрый запуск

### 1. Запуск всей системы:

```bash
# Запуск всех сервисов с фронтендом
docker-compose up --build -d

# Проверка статуса
docker-compose ps
```

### 2. Доступ к системе:

- **Веб-интерфейс**: http://localhost:3000
- **API Gateway**: http://localhost:8080
- **RabbitMQ Management**: http://localhost:15672 (guest/guest)

### 3. Остановка системы:

```bash
docker-compose down
```

## 🎯 Использование веб-интерфейса

Откройте http://localhost:3000 в браузере и используйте кнопки для:

### Payments Service:
1. **Создать аккаунт** - создает новый аккаунт для пользователя
2. **Пополнить баланс** - добавляет средства на аккаунт
3. **Проверить баланс** - показывает текущий баланс

### Orders Service:
1. **Создать заказ** - создает новый заказ
2. **Получить заказы** - показывает список заказов пользователя

## 🔧 API Endpoints

### Payments Service
- `POST /api/payments/accounts` - Создание аккаунта
- `POST /api/payments/accounts/{id}/top-up` - Пополнение баланса
- `GET /api/payments/accounts/{id}/balance` - Проверка баланса

### Orders Service  
- `POST /api/orders` - Создание заказа
- `GET /api/orders?userId={id}` - Получение заказов пользователя

## 📊 Мониторинг

### Логи сервисов:
```bash
# Все логи
docker-compose logs -f

# Конкретный сервис
docker-compose logs -f frontend
docker-compose logs -f api-gateway
docker-compose logs -f orders-api
docker-compose logs -f payments-api
```

### Проверка здоровья:
```bash
# Проверка API Gateway
curl http://localhost:8080/api/orders

# Проверка фронтенда
curl http://localhost:3000
```

## 🧪 Тестирование

### Автоматическое тестирование через веб-интерфейс:
1. Откройте http://localhost:3000
2. Нажмите "Создать аккаунт"
3. Скопируйте Account ID в поле пополнения
4. Нажмите "Пополнить" с суммой 1000
5. Нажмите "Проверить баланс"
6. Нажмите "Создать заказ" с суммой 299.99
7. Нажмите "Получить заказы"

### Тестирование через cURL:
```bash
# 1. Создание аккаунта
curl -X POST http://localhost:8080/api/payments/accounts \
  -H "Content-Type: application/json" \
  -d '{"userId":"11111111-1111-1111-1111-111111111111"}'

# 2. Пополнение баланса (замените account-id)
curl -X POST http://localhost:8080/api/payments/accounts/{account-id}/top-up \
  -H "Content-Type: application/json" \
  -d '{"amount":1000.50}'

# 3. Создание заказа
curl -X POST http://localhost:8080/api/orders \
  -H "Content-Type: application/json" \
  -d '{"userId":"11111111-1111-1111-1111-111111111111","amount":299.99}'
```

## ⚡ Unit Tests

```bash
# Запуск всех тестов
dotnet test

# Тесты Orders Service
dotnet test src/Services/Orders/Orders.Tests/

# Тесты Payments Service  
dotnet test src/Services/Payments/Payments.Tests/
```

## 🔍 Устранение неисправностей

### Проблема: Порт уже используется
```bash
# Найти процесс использующий порт
lsof -i :3000
lsof -i :8080

# Остановить все контейнеры
docker-compose down
```

### Проблема: База данных не подключается
```bash
# Перезапуск с пересборкой
docker-compose down
docker-compose up --build -d

# Проверка логов SQL Server
docker-compose logs sqlserver
```

### Проблема: RabbitMQ недоступен
```bash
# Проверка логов RabbitMQ
docker-compose logs rabbitmq

# Перезапуск только RabbitMQ
docker-compose restart rabbitmq
```

## 📈 Производительность

- **Время старта**: ~45 секунд для полной инициализации
- **Использование памяти**: ~2GB RAM
- **CPU**: Оптимизировано для development

## 🌟 Особенности

✅ **Полная микросервисная архитектура**  
✅ **Веб-интерфейс для тестирования**  
✅ **API Gateway с маршрутизацией**  
✅ **Асинхронные сообщения через RabbitMQ**  
✅ **Transactional Outbox/Inbox patterns**  
✅ **Clean Architecture**  
✅ **CQRS с MediatR**  
✅ **Docker контейнеризация**  
✅ **Unit тесты покрытие**  

---

**🎯 Система готова к использованию и демонстрации!** 