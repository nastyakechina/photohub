# PhotoHub

Социальная сеть для публикации фотографий. Микросервисная система на .NET 8.

## Архитектура

Система состоит из 6 микросервисов + API Gateway:

- **AuthService** — регистрация, авторизация, JWT токены
- **FriendsService** — подписки между пользователями
- **PhotoService** — загрузка фото, метаданные, хранение в MinIO (S3)
- **FeedService** — лента новостей (агрегирует подписки + фото)
- **LikeService** — лайки с Redis-кешированием счётчиков
- **PreviewService** — генерация превью (consumer RabbitMQ событий)
- **ApiGateway** — YARP reverse proxy, JWT-аутентификация, rate limiting

```
Client → ApiGateway (JWT + Rate Limit) → Services
                                        ├── AuthService (PostgreSQL)
                                        ├── FriendsService (PostgreSQL + RabbitMQ publisher)
                                        ├── PhotoService (PostgreSQL + MinIO + RabbitMQ)
                                        ├── FeedService (PostgreSQL + Redis + RabbitMQ consumers)
                                        ├── LikeService (PostgreSQL + Redis)
                                        └── PreviewService (RabbitMQ consumer)

Infra: Prometheus → Grafana, Jaeger (OTLP), RabbitMQ, Redis
```

### FeedService — детали

FeedService реализует push-модель (materialized view) для ленты:

- **feed_items** (PostgreSQL) — денормализованная копия постов для каждого подписчика
- **Consumers**:
  - `PhotoCreatedConsumer` — создаёт feed_items для всех подписчиков автора и инвалидирует Redis-кеш рекомендаций
  - `UserUnfollowedConsumer` — удаляет feed_items отписавшегося и инвалидирует его кеш
- **FeedBackfillService** — одноразовый startup job: если feed_items пусто, ретроспективно заполняет ленту из PhotoService + FriendsService
- **GET /api/feed/{userId}** возвращает две секции:
  - `following` — посты из feed_items (LIMIT 50, ORDER BY CreatedAtUtc DESC)
  - `recommended` — посты друзей друзей, которых пользователь не читает (LIMIT 20)
  - `items` — alias для `following`, для обратной совместимости

### Redis — два применения

| Сервис | Ключи | Назначение |
|---|---|---|
| LikeService | `likes:photo:{photoId}:count` | Счётчики лайков, инкремент/декремент без обращения в БД |
| FeedService | `feed:recommended:{userId}` | Кеш рекомендаций, TTL 15 мин, инвалидация по событиям follow/unfollow и публикации фото |

## Запуск

```bash
cd Photohub
cp .env.example .env
docker compose up --build
```

Сервисы после запуска:

| Сервис         | URL                          |
|----------------|------------------------------|
| API Gateway    | http://localhost:5000         |
| Swagger        | http://localhost:5000/swagger |
| RabbitMQ UI    | http://localhost:15672        |
| Prometheus     | http://localhost:9090         |
| Grafana        | http://localhost:3000         |
| Jaeger UI      | http://localhost:16686        |
| MinIO Console  | http://localhost:9001         |

## Реализованные пункты

### Блок 1 — Межсервисное взаимодействие
- ✅ 6 микросервисов
- ✅ REST API во всех сервисах
- ✅ API Gateway (YARP)
- ✅ RabbitMQ (MassTransit) — async события
- ✅ Event-driven pub/sub — PhotoCreatedEvent
- ✅ Rate limiting на Gateway (100 req/min per IP)

### Блок 2 — Данные и consistency
- ✅ Database per service (5 × PostgreSQL)
- ✅ EF Migrations во всех сервисах
- ✅ Eventual consistency через RabbitMQ
- ✅ Redis distributed cache (LikeService — счётчики лайков; FeedService — кеш рекомендаций, TTL 15 мин)
- ✅ CQRS в LikeService (Commands/Queries разделены)
- ✅ Push-model materialized feed (FeedService → feed_items в PostgreSQL)
- ✅ Рекомендации через friends-of-friends с Redis-кешированием

### Блок 3 — Resilience и Observability
- ✅ JSON структурированные логи (Serilog + CompactJsonFormatter)
- ✅ Health checks (/health)
- ✅ Graceful shutdown
- ✅ Correlation ID propagation
- ✅ Prometheus метрики (/metrics)
- ✅ Grafana dashboard
- ✅ Distributed tracing (Jaeger + OpenTelemetry OTLP)
- ✅ Retry + Circuit breaker (Polly) в FeedService

### Блок 4 — Security и Production
- ✅ Env vars для конфигурации
- ✅ Docker Compose
- ✅ .env в .gitignore
- ✅ Swagger/OpenAPI
- ✅ JWT authentication + validation в API Gateway
- ✅ CI/CD (GitHub Actions)
