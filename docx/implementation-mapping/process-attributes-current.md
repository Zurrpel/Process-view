# Требования ↔ функции (атрибуты, сохраняемые в БД)

Дата: 2026-05-06

Этот документ — основа для сопоставления **требований** и **функций/механизмов реализации**.

На текущий момент в качестве “требований” зафиксированы только **атрибуты, которые основная программа `ProcessView` реально записывает в SQLite**. Дальнейшие требования будут добавляться отдельно.

---

## Класс требований: атрибуты снимка процессов (`ProcessSnapshot`)

| Требование (атрибут + кратко) | Функция / механизм реализации (API / место в коде) |
|---|---|
| `SnapshotId` — идентификатор снимка (UTC, Unix time в мс) | `SQLiteProcessRepository.CreateSnapshotId()` → `DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()` |
| `ProcessId` — идентификатор процесса (PID) | `Toolhelp32.TakeSnapshot()` → WinAPI Toolhelp (`CreateToolhelp32Snapshot`, `Process32First/Next`) → `PROCESSENTRY32.th32ProcessID` |
| `ParentProcessId` — идентификатор родительского процесса (PPID) | `Toolhelp32.TakeSnapshot()` → WinAPI Toolhelp → `PROCESSENTRY32.th32ParentProcessID` |
| `Name` — имя исполняемого файла (без пути) | `Toolhelp32.TakeSnapshot()` → WinAPI Toolhelp → `PROCESSENTRY32.szExeFile` |
| `ThreadCount` — количество потоков (не сохраняется сейчас) | WinAPI Toolhelp → `PROCESSENTRY32.cntThreads` (поле структуры `PROCESSENTRY32` в `Toolhelp32.cs`) |
| `UsageCount` — счётчик использования (не сохраняется сейчас) | WinAPI Toolhelp → `PROCESSENTRY32.cntUsage` (поле структуры `PROCESSENTRY32`) |
| `ModuleId` — идентификатор модуля (не сохраняется сейчас) | WinAPI Toolhelp → `PROCESSENTRY32.th32ModuleID` (поле структуры `PROCESSENTRY32`) |
| `DefaultHeapId` — идентификатор default heap (не сохраняется сейчас) | WinAPI Toolhelp → `PROCESSENTRY32.th32DefaultHeapID` (поле структуры `PROCESSENTRY32`) |
| `PriorityClassBase` — базовый приоритет (не сохраняется сейчас) | WinAPI Toolhelp → `PROCESSENTRY32.pcPriClassBase` (поле структуры `PROCESSENTRY32`) |
| `Flags` — флаги записи Toolhelp (не сохраняется сейчас) | WinAPI Toolhelp → `PROCESSENTRY32.dwFlags` (поле структуры `PROCESSENTRY32`) |

## Класс требований: хранение в SQLite (схема + индексы + запись)

| Требование (что должно быть в БД) | Функция / механизм реализации (SQL / место в коде) |
|---|---|
| Таблица для хранения снимков процессов: `ProcessSnapshot(SnapshotId, ProcessId, ParentProcessId, Name)` | `SQLiteProcessRepository.InitializeSchemaAsync()` → `CREATE TABLE IF NOT EXISTS ProcessSnapshot (...)` |
| Уникальность записи процесса в рамках снимка | `PRIMARY KEY (SnapshotId, ProcessId)` в `InitializeSchemaAsync()` |
| Индекс для быстрого поиска детей по родителю в рамках снимка | `CREATE INDEX IF NOT EXISTS IX_ProcessSnapshot_Snapshot_Parent ON ProcessSnapshot (SnapshotId, ParentProcessId)` в `InitializeSchemaAsync()` |
| Запись данных выполняется транзакцией | `SQLiteProcessRepository.InsertSnapshotAsync(...)` → `BeginTransactionAsync()` → `CommitAsync()` |
| Вставка одной записи процесса в рамках снимка | `SQLiteProcessRepository.InsertSnapshotAsync(...)` → `INSERT INTO ProcessSnapshot (...) VALUES (...)` |

---

## Класс требований: атрибуты сенсора аппаратного мониторинга (`ISensor` / `SensorValue` / `SensorType`)

| Требование (атрибут сенсора + кратко) | Функция / механизм реализации (API / структура в LibreHardwareMonitor) |
|---|---|
| `Identifier` — уникальный идентификатор сенсора | `ISensor.Identifier` (уникальный путь внутри дерева `Computer` → `Hardware` → `Sensor`) |
| `Name` — имя сенсора | `ISensor.Name` (задаётся библиотекой; может быть изменено) |
| `Hardware` — устройство-владелец сенсора | `ISensor.Hardware` (ссылка на `IHardware`: CPU, GPU, Motherboard, Storage и т.п.) |
| `SensorType` — тип данных сенсора | `ISensor.SensorType` (enum `SensorType`: `Voltage`, `Current`, `Power`, `Clock`, `Temperature`, `Load`, `Frequency`, `Fan`, `Flow`, `Control`, `Level`, `Factor`, `Data`, `SmallData`, `Throughput`, `TimeSpan`, `Timing`, `Energy`, `Noise`, `Conductivity`, `Humidity`) |
| `Value` — текущее значение | `ISensor.Value` (`float?`), обновляется после `IHardware.Update()` |


