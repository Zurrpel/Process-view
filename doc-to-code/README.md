## Doc-to-code workspace

Эта директория выделена под процесс **doc → code**: перевод требований/спецификаций в изменения кода предсказуемым способом.

Здесь намеренно разделены:

- `templates/` — шаблоны документов (формат, который заполняется).
- `scripts/` — “скрипты” процесса: пошаговые инструкции для агента по заполнению шаблонов и подготовке документации к внедрению фичи (не исполняемый код).
- `rules/` — правила, гайды и чеклисты для предсказуемых изменений.
- `specs/` — входные спецификации (по одной фиче на файл), которые переводятся в код.

---

## Как пользоваться `doc-to-code/`

### Основной сценарий: “документ фичи → изменения кода”

1) **Создай спецификацию фичи**
   - Добавь файл: `doc-to-code/specs/features/<F-ID>.md`
   - Заполни его по шаблону: `doc-to-code/templates/features/F-XXXX-template.md`

2) **Подготовь документ до уровня “можно кодить” (фаза анализа, без правок кода)**
   - Следуй плейбуку: `doc-to-code/scripts/agent-feature-prep.md`
   - Проверь, что нет “дыр”, влияющих на реализацию:
     - `Acceptance criteria` проверяемые (булевы)
     - `Implementation mapping` указывает конкретные файлы/классы/точки интеграции
     - `Test plan (manual)` опирается на единые команды из `doc-to-code/rules/dev-commands.md`
   - Поставь статус в документе фичи: **Approved plan**

3) **Внеси изменения в код (фаза изменений)**
   - Работай в рамках ограничений: `doc-to-code/rules/agent-guardrails.md`
   - Ориентируйся на точки расширения: `doc-to-code/rules/implementation-guide.md`
   - Если фича затрагивает БД — используй: `doc-to-code/rules/db-schema-and-migrations.md`

4) **Проверь локально**
   - Используй команды из: `doc-to-code/rules/dev-commands.md`

5) **Обнови обязательные артефакты**
   - Следуй чеклисту: `doc-to-code/rules/artifacts-checklist.md`
   - Минимум:
     - обнови статус фичи (например, `Implemented`, затем `Verified`)
     - обнови трассировку “требование → реализация” в `docx/implementation-mapping/*` (если применимо)

---

## Как держать шаблоны и правила актуальными после внедрения фич

Любая новая фича может добавить термины/контракты/паттерны расширения или изменить способ проверки. Чтобы процесс `doc → code` не деградировал, после внедрения фичи обновляй `doc-to-code/` по правилам maintenance-loop:

- План процесса: `doc-to-code/specs/update-feature-templates-plan.md`

Коротко, когда **обязательно** обновлять `doc-to-code/templates/` и `doc-to-code/rules/`:

- если появился новый термин или контракт → `doc-to-code/rules/glossary-and-contracts.md`
- если менялась схема/совместимость SQLite или подход к миграциям → `doc-to-code/rules/db-schema-and-migrations.md`
- если изменились команды сборки/запуска/smoke-check или ожидаемые признаки → `doc-to-code/rules/dev-commands.md`
- если добавились новые точки расширения/папки/паттерны внедрения → `doc-to-code/rules/implementation-guide.md`
- если пришлось расширить/изменить ограничения (guardrails) → `doc-to-code/rules/agent-guardrails.md`
- если выявился пробел в структуре документа фичи → `doc-to-code/templates/features/F-XXXX-template.md` и синхронизация `doc-to-code/scripts/agent-feature-prep.md`

---

## Минимальный пакет документации перед кодингом

См. `doc-to-code/scripts/agent-docs-package.md`.

Вкратце, “MVP” пакет — это:

- `doc-to-code/specs/features/<F-ID>.md` со статусом **Approved plan**
- `doc-to-code/rules/agent-guardrails.md`
- `doc-to-code/rules/glossary-and-contracts.md`
- `doc-to-code/rules/implementation-guide.md`
- `doc-to-code/rules/dev-commands.md`

