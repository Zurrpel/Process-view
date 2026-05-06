## Implementation guide (карта точек расширения)

Дата: 2026-05-06

Цель: зафиксировать “архитектурное оглавление” проекта, чтобы внедрение новых фич было предсказуемым: куда добавлять сбор данных, где схема БД, где CLI и т.п.

Источник истины для этого гайда — текущий код проекта (см. ссылки на файлы ниже).

---

### 1) Структура проекта (по текущему репозиторию)

Проект: `ProcessView/ProcessView.csproj` (TargetFramework: `net8.0`, тип: `Exe`).

Папки и ответственность:

- `ProcessView/`
  - консольное приложение (точка входа, конфиг, orchestrator цикла).
- `ProcessView/Domain/`
  - доменные модели (структуры данных снимка).
- `ProcessView/Interop/`
  - низкоуровневый сбор данных из Windows (WinAPI Toolhelp через P/Invoke).
- `ProcessView/Data/`
  - хранение и схема SQLite (инициализация таблиц + запись снимков).

---

### 2) Основной цикл (entrypoint → сбор → запись → пауза)

Точка входа: `ProcessView/Program.cs`.

Фактический цикл:

- конфиг: `AppConfig.FromEnvironmentAndArgs(args)` (`ProcessView/AppConfig.cs`)
- репозиторий: `new SQLiteProcessRepository(config.DatabasePath)` (`ProcessView/Data/SQLiteProcessRepository.cs`)
- инициализация схемы: `InitializeSchemaAsync()`
- периодический цикл:
  - создание `snapshotId`: `CreateSnapshotId()` (Unix time ms, UTC)
  - сбор процессов: `Toolhelp32.TakeSnapshot()` (`ProcessView/Interop/Toolhelp32.cs`)
  - запись: `InsertSnapshotAsync(snapshotId, processes)`
  - задержка: `Task.Delay(intervalMs, cancellationToken)`

---

### 3) Конфигурация и публичная поверхность (CLI/ENV)

Файл: `ProcessView/AppConfig.cs`.

Поддерживаемые параметры CLI (текущее состояние):

- `--db <path>`: путь к SQLite файлу.
- `--interval-ms <int>`: интервал съёма снимка в миллисекундах.

Поддерживаемые переменные окружения (текущее состояние):

- `PROCESS_VIEW_INTERVAL_MS`: если задана и валидна, переопределяет интервал.

Примечание:

- В текущей реализации нет `--help` и нет валидации “неизвестных аргументов”: неизвестные аргументы игнорируются (это наблюдение следует из простого `for` + `switch` по известным ключам в `AppConfig.FromEnvironmentAndArgs`).

---

### 4) Сбор данных (Interop): где добавлять новые поля процесса

Файл: `ProcessView/Interop/Toolhelp32.cs`.

Текущие поля процесса, которые собираются и передаются дальше:

- `ProcessId`: `PROCESSENTRY32.th32ProcessID`
- `ParentProcessId`: `PROCESSENTRY32.th32ParentProcessID`
- `Name`: `PROCESSENTRY32.szExeFile`

Также в структуре `PROCESSENTRY32` уже присутствуют поля, которые потенциально можно начать сохранять (сейчас не сохраняются):

- `cntThreads`, `cntUsage`, `th32ModuleID`, `th32DefaultHeapID`, `pcPriClassBase`, `dwFlags`

---

### 5) Модель домена: где отражать новые атрибуты

Файл: `ProcessView/Domain/ProcessInfo.cs`.

Текущая модель:

- `ProcessId` (int)
- `ParentProcessId` (int)
- `Name` (string)

Если фича требует добавления атрибутов процесса, изменения ожидаемо начинаются с расширения `ProcessInfo` и источника данных (`Toolhelp32` или иной interop), затем отражаются в схеме/записи SQLite.

---

### 6) Хранилище: SQLite схема и запись

Файл: `ProcessView/Data/SQLiteProcessRepository.cs`.

Текущая схема (создаётся при старте):

- таблица `ProcessSnapshot(SnapshotId, ProcessId, ParentProcessId, Name)`
- `PRIMARY KEY (SnapshotId, ProcessId)`
- индекс `IX_ProcessSnapshot_Snapshot_Parent` по `(SnapshotId, ParentProcessId)`

Запись:

- `InsertSnapshotAsync` делает транзакцию и вставляет строки `INSERT INTO ProcessSnapshot ...` для каждого процесса.

---

### 7) Где вести трассировку “требование → реализация”

Текущее сопоставление атрибутов и реализации находится в:

- `docx/implementation-mapping/process-attributes-current.md`

Этот документ должен обновляться при изменениях структуры данных/схемы/сборщиков.

