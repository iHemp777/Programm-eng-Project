# SurveyService

Сервис опросов (Survey Service) — ASP.NET Core Web API + EF Core + PostgreSQL.

## Что здесь реализовано

- **Модель опроса (`Survey`)**:
  - статус (`Draft/Published/Closed/Archived`)
  - доступ (`Public` / `PrivateByLink`)
  - анонимность (`IsAnonymous`)
  - временные окна (`StartsAt/EndsAt`)
  - токен доступа для приватных опросов (`InviteToken`)
  - аудит (`CreatedAt/UpdatedAt/CompletedAt`)
- **Связи**:
  - `Survey` → `Question` → `Option` (каскадное удаление)
- **API**:
  - получение списков (краткие DTO)
  - получение детального опроса
  - приватный доступ по токену
  - создание/обновление (через DTO)
  - статистика
- **Миграции EF Core**: папка `Migrations/`

## Где смотреть код

- Модель: `Models/Survey.cs`, `Models/Question.cs`, `Models/Option.cs`
- DTO: `DTOs/SurveyDtos.cs`
- Контроллер: `Controllers/SurveysController.cs`
- EF Core: `Data/AppDbContext.cs`
- Startup: `Program.cs`
- Примеры запросов: `SurveyService.http`

## Конфигурация

Строка подключения к PostgreSQL задаётся в:

- `appsettings.json` → `ConnectionStrings:Default`
- `appsettings.Development.json` → `ConnectionStrings:Default`

Пример (локально):

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=survey_service_dev;Username=postgres;Password=postgres"
  }
}
```

## Как запустить локально (высокоуровнево)

1. Поднять PostgreSQL и создать БД (или дать права на авто-создание).
2. Применить миграции:

```bash
dotnet ef database update
```

3. Запустить сервис:

```bash
dotnet run
```

4. Открыть Swagger по `/swagger` и/или использовать `SurveyService.http`.

## Как запустить через Docker (PostgreSQL + pgAdmin + API)

В этой папке есть `docker-compose.yml`, который поднимет:

- `db`: PostgreSQL (порт хоста `5433` → контейнер `5432`)
- `api`: SurveyService (порт хоста `5186` → контейнер `8080`)
- `pgadmin`: pgAdmin4 (порт хоста `5050`)

Запуск:

```bash
docker compose up -d --build
```

Полезные ссылки:

- Swagger: `http://localhost:5186/swagger`
- pgAdmin4: `http://localhost:5050` (логин `admin@local`, пароль `admin`)

Примечания:

- Строка подключения внутри контейнера задаётся через переменную окружения `ConnectionStrings__Default` и указывает на `Host=db`.
- Миграции EF Core применяются автоматически при старте сервиса.

