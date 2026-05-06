## Схема БД и миграции (правила изменений)

Дата: 2026-05-06

Цель: зафиксировать текущую схему SQLite и правила её изменения, чтобы внедрение фич не ломало данные и оставалось сопровождаемым.

Источник требований к правилам: `doc-to-code/specs/doc-to-code-automation-plan.md` (раздел 4).

---

### 1) Текущая схема (факт из кода)

Текущая версия приложения при старте выполняет `CREATE TABLE IF NOT EXISTS` для таблицы `ProcessSnapshot`:

- `SnapshotId INTEGER NOT NULL`
- `ProcessId INTEGER NOT NULL`
- `ParentProcessId INTEGER NOT NULL`
- `Name TEXT NOT NULL`
- `PRIMARY KEY (SnapshotId, ProcessId)`

И создаёт индекс:

- `IX_ProcessSnapshot_Snapshot_Parent` по `(SnapshotId, ParentProcessId)`

Основание: `SQLiteProcessRepository.InitializeSchemaAsync()` в `ProcessView/Data/SQLiteProcessRepository.cs`.

---

### 2) Текущее поведение по миграциям (факт из кода)

- Автоматических миграций сейчас нет.
- При запуске выполняется только “инициализация при отсутствии таблицы” (через `IF NOT EXISTS`), то есть изменения существующей схемы не применяются автоматически.

Основание: `InitializeSchemaAsync()` выполняет только `CREATE ... IF NOT EXISTS`.

---

### 3) Правила изменения схемы (нужно зафиксировать до внедрения фич, placeholder)

Ниже — структура правил, которые должны быть явно утверждены проектом. Эти пункты не выводятся однозначно из текущего кода и требуют решения.

#### 3.1 Можно ли менять существующие таблицы или только добавлять новые?

- [TBD] (решение проекта)

#### 3.2 Как версионировать схему?

Варианты, которые можно выбрать (нужно одно решение):

- [TBD] `PRAGMA user_version`
- [TBD] Таблица `SchemaVersion(version INTEGER NOT NULL, applied_at_ms INTEGER NOT NULL, ...)`

#### 3.3 Принцип миграций (минимальный, ручной, без “магии”)

- [TBD] папка `migrations/` с упорядоченными `.sql` файлами
- [TBD] команда/скрипт для применения миграций локально

---

### 4) Трассировка требований на схему

Текущее сопоставление атрибутов и реализации уже ведётся в:

- `docx/implementation-mapping/process-attributes-current.md`

