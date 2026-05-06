## Локальные команды (единый “инструментальный слой” без CI)

Дата: 2026-05-06

Цель: один и тот же набор команд для всех изменений (вручную и агентом), чтобы “проверяемость” не зависела от контекста.

Источник: требование из `doc-to-code/specs/doc-to-code-automation-plan.md` (раздел 8).

---

### Prerequisites

- .NET SDK 8.x
  - основание: `ProcessView/ProcessView.csproj` использует `TargetFramework net8.0`.

---

### Команда “собрать”

Из корня репозитория:

```powershell
dotnet build .\ProcessView\ProcessView.csproj -c Release
```

---

### Команда “базовая проверка” (smoke-check)

Цель: убедиться, что приложение запускается, создаёт/открывает SQLite и пишет хотя бы один снимок.

Из корня репозитория:

```powershell
dotnet run --project .\ProcessView\ProcessView.csproj -- --interval-ms 250 --db .\_local\processview-smoke.db
```

Ожидаемые признаки:

- в stdout есть строка вида `ProcessView starting. DB: ..., interval: ... ms` (см. `ProcessView/Program.cs`);
- затем появляется `Snapshot <id>: <n> processes recorded in <ms> ms` минимум один раз;
- при остановке (Ctrl+C) видны строки `Cancellation requested...` и `ProcessView stopped.`

Примечание:

- текущая версия не поддерживает `--help` (это можно добавить отдельной фичей через шаблон).

