## Feature

### ID
F-0001

### Goal
Начать сохранять в SQLite расширенные атрибуты процесса, которые уже доступны из WinAPI Toolhelp (`PROCESSENTRY32`), чтобы данные снимка процессов были богаче без изменения источника сбора.

### Scope
Входит:
- расширение доменной модели процесса новыми полями, которые уже присутствуют в `PROCESSENTRY32`: `ThreadCount`, `UsageCount`, `ModuleId`, `DefaultHeapId`, `PriorityClassBase`, `Flags`
- расширение таблицы `ProcessSnapshot` соответствующими столбцами и запись этих значений при вставке снимка
- сохранение текущих контрактов `SnapshotId` и ключа уникальности `(SnapshotId, ProcessId)`

Не входит:
- добавление новых источников данных кроме Toolhelp (например, OpenProcess/Performance Counters/WMI)
- изменение стратегии идентификации процессов во времени (за пределами текущего ключа записи в снимке)
- изменения пользовательского интерфейса/визуализации (если она существует вне текущего CLI-цикла)

### User flows
1) Пользователь запускает `ProcessView` с уже существующими параметрами `--db` и `--interval-ms`.
   - Ожидаемо: в SQLite, в каждой строке `ProcessSnapshot` дополнительно заполнены новые колонки расширенных атрибутов (см. “Data changes”).

### Data changes
Таблица: `ProcessSnapshot`

Добавить столбцы:
- `ThreadCount` (INTEGER, NOT NULL)
- `UsageCount` (INTEGER, NOT NULL)
- `ModuleId` (INTEGER, NOT NULL)
- `DefaultHeapId` (INTEGER, NOT NULL)
- `PriorityClassBase` (INTEGER, NOT NULL)
- `Flags` (INTEGER, NOT NULL)

Контракты, которые не меняются:
- `SnapshotId` как Unix time ms (UTC)
- `PRIMARY KEY (SnapshotId, ProcessId)`
- индекс `IX_ProcessSnapshot_Snapshot_Parent` по `(SnapshotId, ParentProcessId)`

Основание (текущее состояние): “не сохраняется сейчас” для этих полей и текущая схема `ProcessSnapshot(SnapshotId, ProcessId, ParentProcessId, Name)` зафиксированы в `docx/implementation-mapping/process-attributes-current.md`.

### Public surface
Не меняется (CLI/ENV остаются как сейчас).

### Acceptance criteria
- В схеме SQLite после запуска приложения присутствуют колонки `ThreadCount`, `UsageCount`, `ModuleId`, `DefaultHeapId`, `PriorityClassBase`, `Flags` в таблице `ProcessSnapshot`.
- При записи снимка каждая вставленная строка `ProcessSnapshot` содержит значения новых колонок, соответствующие данным из `PROCESSENTRY32` для этого процесса в момент съёма снимка.
- Существующие колонки (`SnapshotId`, `ProcessId`, `ParentProcessId`, `Name`), первичный ключ и индекс остаются совместимыми с текущим контрактом схемы.
- Приложение продолжает корректно создавать снимки и записывать их транзакцией (как и раньше).

### Constraints
- Нельзя менять формат генерации `SnapshotId` (Unix time ms, UTC), т.к. это уже контракт текущей реализации.
- Нельзя менять ключ уникальности записи процесса в рамках снимка: `(SnapshotId, ProcessId)`.
- Нельзя менять смысл текущих полей `ProcessId`, `ParentProcessId`, `Name` (они должны оставаться данными Toolhelp `PROCESSENTRY32`).

### Implementation mapping
Точки внедрения (по “карте точек расширения”):
- `ProcessView/Interop/Toolhelp32.cs`: расширить сбор/маппинг из `PROCESSENTRY32` для полей `cntThreads`, `cntUsage`, `th32ModuleID`, `th32DefaultHeapID`, `pcPriClassBase`, `dwFlags`.
  - Основание: перечисление доступных (но не сохраняемых) полей в `PROCESSENTRY32` зафиксировано в `docx/implementation-mapping/process-attributes-current.md`.
- `ProcessView/Domain/ProcessInfo.cs`: добавить свойства доменной модели для новых атрибутов процесса.
  - Основание: текущая модель содержит только `ProcessId`, `ParentProcessId`, `Name` и является местом для расширения атрибутов (см. `doc-to-code/rules/implementation-guide.md`).
- `ProcessView/Data/SQLiteProcessRepository.cs`:
  - `InitializeSchemaAsync()`: добавить новые колонки в `CREATE TABLE ...` (или эквивалентную миграционную стратегию, если она появится).
  - `InsertSnapshotAsync(...)`: расширить `INSERT INTO ProcessSnapshot (...) VALUES (...)` и параметры вставки на новые поля.
  - Основание: схема и вставка снимков локализованы в этом репозитории (см. `doc-to-code/rules/implementation-guide.md`).
- `docx/implementation-mapping/process-attributes-current.md`: обновить строки требований “не сохраняется сейчас” на “сохраняется”, и добавить ссылки на новые места в коде/SQL после реализации.
  - Основание: чеклист артефактов требует обновления маппинга требований и фич-документа.

### Test plan (manual)
1) Запустить `ProcessView` на коротком интервале (например, 1–2 секунды) и дать сделать 1–2 снимка в новую/пустую БД.
2) Проверить схему SQLite: таблица `ProcessSnapshot` содержит новые колонки.
3) Проверить данные: выбрать несколько строк из `ProcessSnapshot` и убедиться, что новые поля заполнены (не NULL) и имеют целочисленные значения.

---

## Status

### Current status
Draft

### Notes / decisions
- Этот документ намеренно ограничен атрибутами, уже доступными в `PROCESSENTRY32`, чтобы не расширять источники данных.
- Типы и nullability для новых полей заданы как NOT NULL, т.к. `PROCESSENTRY32` предоставляет их как числовые поля структуры (см. текущий маппинг требований и реализации в `docx/implementation-mapping/process-attributes-current.md`). Если в коде окажется, что какое-то значение невозможно получить/маппится условно, требования по nullability нужно уточнить и обновить этот документ.
