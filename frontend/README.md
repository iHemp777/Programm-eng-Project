# Frontend (UI)

Интерфейс для Survey/Voting приложения.

## Запуск

1) Убедитесь, что backend поднят (через `service/docker-compose.yml`) и API Gateway доступен на `http://localhost:5000`.

2) В папке `frontend`:

```bash
npm install
npm run dev
```

Откройте `http://localhost:5173`.

## Настройка API

UI ходит в API Gateway. Можно переопределить адрес через переменную окружения:

- `VITE_API_BASE_URL` (см. `.env.example`)

