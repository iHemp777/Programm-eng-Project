# VotingService

`VotingService` — микросервис системы голосования, который принимает голоса респондентов, защищает от повторного голосования и отдает агрегированную статистику по опросам.

Сервис реализован на `ASP.NET Core Web API (.NET 8)` с использованием:

- `Entity Framework Core` + `PostgreSQL` для хранения голосов,
- `Redis` для кэширования статистики и публикации событий,
- HTTP-интеграции с `SurveyService` для проверки существования опроса.

---

## Назначение сервиса

В контексте общей архитектуры `VoteSystem` сервис отвечает за:

- прием и валидацию голоса от респондента;
- проверку, что опрос существует (синхронный HTTP вызов в `SurveyService`);
- предотвращение повторного голосования одного пользователя в рамках одного опроса;
- построение и отдачу статистики (количество голосов по вариантам ответов);
- кэширование статистики для снижения нагрузки на БД;
- публикацию события после успешного голосования (`votes:created`) в Redis.

---

## Функциональность

Реализованные функции:

- `POST /api/votes` — отправка голоса;
- `GET /api/votes/surveys/{surveyId}/results` — получение результатов по опросу;
- `GET /api/votes/surveys/{surveyId}/has-voted?voterId={id}` — проверка, голосовал ли пользователь.

Ключевые правила:

- один пользователь (`VoterId`) может проголосовать в опросе (`SurveyId`) только один раз;
- в одном голосе нельзя передать два ответа на один и тот же вопрос;
- результаты кэшируются в Redis на 5 минут;
- после нового голоса кэш результатов опроса инвалидируется.

---

## Структура проекта

Основные файлы и папки:

- `Program.cs` — DI, middleware, подключение EF, Redis, HTTP-клиента к `SurveyService`;
- `Controllers/VotesController.cs` — REST API для голосования и результатов;
- `Data/AppDbContext.cs` — EF Core контекст и конфигурация модели;
- `Models/Vote.cs` — сущность голоса;
- `Models/VoteAnswer.cs` — сущность ответа в рамках голоса;
- `DTOs/VoteDtos.cs` — контракты API запросов/ответов;
- `Services/SurveyClient.cs` — HTTP-клиент проверки существования опроса;
- `Services/StatisticsCacheService.cs` — работа с Redis-кэшем и событиями;
- `Migrations/` — миграции базы данных.

---

## Модель данных

### `Vote`

Один факт голосования пользователя в конкретном опросе:

- `Id` — PK;
- `SurveyId` — идентификатор опроса;
- `VoterId` — идентификатор респондента;
- `VotedAt` — время голосования (`UTC`);
- `Answers` — список ответов (`VoteAnswer`).

### `VoteAnswer`

Ответ на конкретный вопрос в рамках голоса:

- `Id` — PK;
- `QuestionId` — идентификатор вопроса;
- `OptionId` — выбранный вариант ответа;
- `VoteId` — FK на `Vote`.

### Индексы и ограничения

В `AppDbContext` настроены:

- уникальный индекс `UX_Votes_SurveyId_VoterId` — защита от дублей;
- индекс `IX_Votes_SurveyId` — ускорение выборки по опросу;
- индекс `IX_VoteAnswers_QuestionId_OptionId` — ускорение агрегаций.

---

## API

## 1) Отправить голос

`POST /api/votes`

Пример запроса:

```json
{
  "surveyId": 1,
  "voterId": 1001,
  "answers": [
    { "questionId": 1, "optionId": 2 },
    { "questionId": 2, "optionId": 5 }
  ]
}
```

Поведение:

- проверяет корректность DTO;
- проверяет отсутствие дублирующихся `QuestionId` в `answers`;
- проверяет существование опроса в `SurveyService`;
- проверяет, что пользователь еще не голосовал;
- сохраняет голос в БД;
- инвалидирует Redis-кэш результатов;
- публикует событие `votes:created`.

Коды ответов:

- `201 Created` — голос принят;
- `400 Bad Request` — ошибка валидации/некорректные данные;
- `404 Not Found` — опрос не найден;
- `409 Conflict` — пользователь уже голосовал.

## 2) Получить результаты по опросу

`GET /api/votes/surveys/{surveyId}/results`

Пример ответа:

```json
{
  "surveyId": 1,
  "totalVotes": 12,
  "questions": [
    {
      "questionId": 1,
      "options": [
        { "optionId": 1, "votesCount": 3 },
        { "optionId": 2, "votesCount": 9 }
      ]
    }
  ]
}
```

Поведение:

- проверяет существование опроса;
- сначала пытается вернуть результат из Redis-кэша;
- при отсутствии кэша строит агрегаты из PostgreSQL;
- сохраняет результат в Redis на 5 минут.

Коды ответов:

- `200 OK` — результаты получены;
- `404 Not Found` — опрос не найден.

## 3) Проверить, голосовал ли пользователь

`GET /api/votes/surveys/{surveyId}/has-voted?voterId=1001`

Пример ответа:

```json
{
  "surveyId": 1,
  "voterId": 1001,
  "hasVoted": true
}
```

Код ответа:

- `200 OK`.

---

## Интеграции

### Survey Service (синхронно)

Перед приемом голоса и перед выдачей результатов сервис делает HTTP-запрос:

- `GET {SurveyServiceBaseUrl}/api/surveys/{surveyId}`

Если `SurveyService` возвращает неуспех, `VotingService` считает опрос отсутствующим.

### Redis (асинхронно + кэш)

Используется для:

- кэша результатов: ключ `survey:{surveyId}:results`;
- события после голосования: канал `votes:created`.

Формат полезной нагрузки события:

```json
{
  "surveyId": 1,
  "voteId": 123,
  "occurredAt": "2026-04-13T19:12:00Z"
}
```

---

## Конфигурация

Настройки в `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5434;Database=voting_service;Username=postgres;Password=***",
    "Redis": "localhost:6379"
  },
  "Services": {
    "SurveyService": {
      "BaseUrl": "http://localhost:5001"
    }
  }
}
```

Параметры:

- `ConnectionStrings:Default` — строка подключения PostgreSQL;
- `ConnectionStrings:Redis` — адрес Redis;
- `Services:SurveyService:BaseUrl` — базовый URL сервиса опросов.

---

## Запуск локально

### Предварительные условия

- установлен `.NET SDK 8`;
- доступен PostgreSQL;
- доступен Redis;
- запущен `SurveyService`.

### Шаги

1. Перейти в папку сервиса:

```bash
cd service/voting-service/VotingService
```

2. Применить миграции:

```bash
dotnet ef database update
```

3. Запустить API:

```bash
dotnet run --urls http://localhost:5002
```

4. Открыть Swagger:

- `http://localhost:5002/swagger`

Также можно использовать файл запросов:

- `VotingService.http`

---

## Запуск через Docker Compose

В папке сервиса есть `docker-compose.yml`, который поднимает:

- `db` — PostgreSQL (`5434`);
- `redis` — Redis (`6379`);
- `api` — VotingService (`5002`).

Запуск:

```bash
docker compose up -d --build
```

Остановка:

```bash
docker compose down
```

---

## Проверка работоспособности

Базовая проверка:

1. Убедиться, что в `SurveyService` есть опрос с нужным `surveyId`;
2. Вызвать `POST /api/votes` с корректным телом;
3. Повторно отправить тот же голос — получить `409 Conflict`;
4. Вызвать `GET /api/votes/surveys/{surveyId}/results` и проверить агрегацию;
5. Вызвать `GET /api/votes/surveys/{surveyId}/has-voted?voterId=...`.

---

## Технические детали реализации

- При старте сервиса миграции БД применяются автоматически (`Database.Migrate()` в `Program.cs`);
- кэш инвалидируется только для конкретного опроса, по которому был принят новый голос;
- дополнительно к бизнес-проверке дублей используется ограничение БД (защита от гонок);
- время в сервисе фиксируется в `UTC`.
