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
                                        ├── FriendsService (PostgreSQL)
                                        ├── PhotoService (PostgreSQL + MinIO + RabbitMQ)
                                        ├── FeedService (HTTP → Friends + Photo, Polly)
                                        ├── LikeService (PostgreSQL + Redis)
                                        └── PreviewService (RabbitMQ consumer)

Infra: Prometheus → Grafana, Jaeger (OTLP), RabbitMQ, Redis
```

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
- ✅ Redis distributed cache (LikeService)
- ✅ CQRS в LikeService (Commands/Queries разделены)

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
