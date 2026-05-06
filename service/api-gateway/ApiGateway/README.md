# ApiGateway

`ApiGateway` — reverse proxy для маршрутизации внешних запросов к микросервисам.

## Маршруты

- `/api/surveys/**` -> `survey-api:8080`
- `/api/votes/**` -> `voting-api:8080`
- `/health` -> проверка здоровья gateway

## Запуск в общем стеке

Из папки `service`:

```bash
docker compose up -d --build
```

Gateway публикуется на порту `5000`.
