## Глоссарий и контракты домена

Дата: 2026-05-06

Цель: закрепить термины и стабильные контракты, чтобы требования и код использовали одну и ту же модель.

Источник: `doc-to-code/specs/doc-to-code-automation-plan.md` (раздел 3).

---

### Термины (текущее состояние, извлечённое из кода)

- **Процесс**: запись, полученная из WinAPI Toolhelp (`PROCESSENTRY32`) и представленная как `ProcessInfo` (`ProcessView/Domain/ProcessInfo.cs`).
- **Снимок (snapshot)**: один проход цикла, в котором собирается список процессов и пишется в SQLite.
- **SnapshotId**: идентификатор снимка, который сейчас равен Unix time в миллисекундах (UTC), см. `SQLiteProcessRepository.CreateSnapshotId()` (`ProcessView/Data/SQLiteProcessRepository.cs`).
- **PID / ProcessId**: идентификатор процесса в Windows, поле `PROCESSENTRY32.th32ProcessID`, сохраняется в БД как `ProcessId`.
- **PPID / ParentProcessId**: идентификатор родительского процесса, поле `PROCESSENTRY32.th32ParentProcessID`, сохраняется в БД как `ParentProcessId`.
- **Name**: имя исполняемого файла процесса (без пути), поле `PROCESSENTRY32.szExeFile`, сохраняется в БД как `Name`.

---

### Контракты (то, на что уже опирается текущая реализация)

#### Контракт 1: схема таблицы `ProcessSnapshot`

Текущая версия приложения создаёт таблицу `ProcessSnapshot` со столбцами:

- `SnapshotId` (INTEGER, NOT NULL)
- `ProcessId` (INTEGER, NOT NULL)
- `ParentProcessId` (INTEGER, NOT NULL)
- `Name` (TEXT, NOT NULL)

и ключом:

- `PRIMARY KEY (SnapshotId, ProcessId)`

а также индексом:

- `IX_ProcessSnapshot_Snapshot_Parent` по `(SnapshotId, ParentProcessId)`

Основание: `SQLiteProcessRepository.InitializeSchemaAsync()` (`ProcessView/Data/SQLiteProcessRepository.cs`).

#### Контракт 2: формат `SnapshotId`

`SnapshotId` сейчас генерируется как `DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()`.

Основание: `SQLiteProcessRepository.CreateSnapshotId()` (`ProcessView/Data/SQLiteProcessRepository.cs`).

---

### Стабильные ключи аналитики (текущее состояние)

В текущей схеме и модели данных уникальность записи в рамках снимка обеспечивается композитным ключом `(SnapshotId, ProcessId)`.

Основание: `PRIMARY KEY (SnapshotId, ProcessId)` в `InitializeSchemaAsync()`.

Примечание:

- стратегия “Name vs PID” и правила анализа данных во времени пока не зафиксированы как контракт; их следует формализовать отдельными требованиями, если на это начнёт опираться аналитика.

