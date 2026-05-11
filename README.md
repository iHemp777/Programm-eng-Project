# Programm-eng-Project

Микросервисный стек для опросов и голосования: **Survey Service**, **Voting Service**, **API Gateway** (YARP), PostgreSQL, Redis.

## Структура

| Компонент | Путь | Порт в Docker Compose (хост) |
|-----------|------|------------------------------|
| API Gateway | `service/api-gateway/ApiGateway` | 5000 |
| Survey Service | `service/servey-service/ServeyService` | 5001 |
| Voting Service | `service/voting-service/VotingService` | 5002 |

Подробности по каждому сервису — в локальных `README.md` внутри этих папок.

## Быстрый запуск всего стека

```bash
cd service
docker compose up -d --build
```

- Шлюз: http://localhost:5000/health  
- Swagger опросов: http://localhost:5001/swagger  
- Swagger голосования: http://localhost:5002/swagger  
- Сводная страница ссылок: http://localhost:5000/swagger  

Маршрутизация через шлюз: `/api/surveys/**` и `/api/votes/**` (см. `service/api-gateway/ApiGateway/appsettings.json`).

## Тесты

```bash
dotnet test service/SurveyService.Tests/SurveyService.Tests.csproj
dotnet test service/VotingService.Tests/VotingService.Tests.csproj
dotnet test service/ApiGateway.Tests/ApiGateway.Tests.csproj
```

## Git hooks

См. [.githooks/README.md](.githooks/README.md): `pre-commit` с проверкой сборки всех проектов.

## Требования

- .NET 8 SDK  
- Docker (для compose)  
