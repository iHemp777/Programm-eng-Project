# Git hooks (репозиторий)

Один общий hook `pre-commit`: перед коммитом выполняется `dotnet build` для всех сервисов и тестовых проектов.

## Подключение один раз на клоне

Из корня репозитория:

```bash
git config core.hooksPath .githooks
```

В Git for Windows команда такая же (выполнять в bash или PowerShell из корня репо).

## Права на Unix

```bash
chmod +x .githooks/pre-commit
```

## Отключить

```bash
git config --unset core.hooksPath
```
