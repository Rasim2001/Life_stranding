# Скилы: что включено и почему

**Статус:** действующий. Ревизия 11.09.2026.

Скилы приходят из трёх мест: папка проекта `.claude/skills/` (36 штук), плагин
`unity@claude-plugins-official` (29 штук, лежит в
`~/.claude/plugins/cache/claude-plugins-official/unity/0.1.2-beta/skills/`) и
пользовательская папка `~/.claude/skills/` (2 штуки).

Рубильник — блок `skillOverrides` в `.claude/settings.local.json`. Ключ там — это
**`name:` из frontmatter, а не имя папки**. У пользовательских скилов они расходятся:
папка `3dsmax-mcp-dev` → имя `3dsmax-mcp`, папка `unity-mcp-skill` → имя
`unity-mcp-orchestrator`.

Отключение убирает скил из списка целиком: он не грузится и не срабатывает
автоматически. Возврат — снять строку из `skillOverrides`, действует сразу,
без перезапуска.

## Принцип отбора

Скил остаётся включённым, если он покрывает **текущую или ближайшую** работу и
**не дублирует** более специфичный скил проекта. Общеобразовательные обзоры Unity
выключены: они дублируют то, что в проекте уже решено и записано в `docs/`.

Экономия контекста мотивом не является. Замер от 06.08.2026: весь блок Skills —
5.5k, из них project-скилы 2.06k, остальное плагины и встроенные, на которые
`skillOverrides` не влияет. Настоящий вес контекста — MCP-тулы, а не скилы.
Смысл ревизии в том, чтобы агент не тянул нерелевантный скил и не советовал по нему.

## Включено

**Пайплайн задач** (`disable-model-invocation`, запускает только владелец):
`grill-me`, `to-spec`, `to-tickets`, `to-docs`, `handoff`, `review-rules`. Плюс `grilling`,
`spec-review`, `graphify`.

`review-rules` — ревью кода по `docs/code-quality.md`; заменяет встроенный `/code-review`,
который регламент не читает (фича `code-quality`, 26.09.2026).

**Регламенты проекта:** `unity-verification-loop`, `urp-shader-authoring`,
`zenject-conventions`, `r3-patterns`.

**Unity по фактам кодовой базы:**

| Скил | Чем подтверждён |
|---|---|
| `unity-lighting-vfx` | APV в сцене, предстоящая работа по FX; покрывает свет, партиклы, VFX Graph |
| `unity-cinemachine` | Cinemachine 3.1.4, 13 файлов кода, камера в паспорте паука |
| `unity-audio` | предстоящая работа по SFX и музыке |
| `unity-3d-math` | движение и камера паука |
| `unity-physics-queries` | рейкасты паука по геометрии |
| `unity-lifecycle` | порядок инициализации, Zenject |
| `unity-scene-assets` | аддитивные сцены, сегменты и чанки |
| `unity-state-machines` | `Assets/Scripts/Infastructure/States/` |
| `unity-data-driven` | `SpiderData.asset`, `GameStaticData` |
| `unity-async-patterns` | UniTask в 33 файлах |
| `unity-input-correctness` | Input System 1.19 |

**Пользовательские:** `unity-mcp-orchestrator` (папка `unity-mcp-skill`) — работа
через MCP for Unity, дополняет `docs/agents/unity-mcp-routing.md`.

**Из плагина Unity** (отключить поштучно нельзя, см. ниже) по делу:
`urp-postprocessing`, `validate-urp-render-graph-renderer-feature`,
`shader-graph-create-custom-node`, `optimize-audio`, `audio-setup-mixers`,
`optimize-text-mesh-pro`, `ui-ugui`, `unity-cli`, `unity-package-management`.

## В архиве

Выключены через `skillOverrides`. Причина — в колонке справа.

| Скил | Почему выключен | Когда вернуть |
|---|---|---|
| `unity-npc-behavior` | NPC в проекте нет: ни perception, ни patrol, ни faction; «слоны» — это подбираемые продукты | появится противник или спутник |
| `unity-game-architecture` | выбор DI уже сделан, специфика записана в `zenject-conventions` | пересмотр архитектуры целиком |
| `unity-save-system` | `SaveLoadService` уже написан и не в работе | задача по сохранениям или миграции формата |
| `unity-ai-navigation` | пакет `com.unity.ai.navigation` стоит, но в коде ноль обращений к NavMesh | появится навигация |
| `unity-animation` | Animator в 15 файлах, но задач по анимации сейчас нет | работа по анимации или rigging |
| `unity-editor-tools` | окно `SpiderRig/Scenes` написано и стабильно | доработка Editor-тулинга |
| `unity-graphics` | перекрывается `urp-shader-authoring` и `unity-lighting-vfx` | — |
| `unity-physics` | перекрывается `unity-physics-queries` | работа с суставами, тканью, транспортом |
| `unity-input` | перекрывается `unity-input-correctness` | — |
| `unity-ui` | перекрывается `ui-ugui` из плагина | — |
| `unity-foundations`, `unity-scripting` | базовый ликбез по Unity и C# | — |
| `unity-performance` | профилирование идёт через нативный MCP | задача по оптимизации |
| `3dsmax-mcp` | MCP-сервера 3ds Max в сессии нет | подключишь Max обратно |

## Открытый вопрос: скилы плагина

Нерелевантных в плагине двадцать — 2D, тайлмапы, спрайты, реклама, IAP,
мультиплеер, Vivox, локализация, web-билд, создание нового проекта.

Работает ли `skillOverrides` на скилы плагина — **не проверено**. Замер от 06.08.2026
показал, что на плагин `anthropic-skills` он не влияет. Их имена всё равно внесены
в `skillOverrides` 11.09.2026 как эксперимент: если в следующей сессии они пропадут
из списка — механизм работает на плагины тоже; если останутся — строки можно удалить,
а единственным рычагом останется отключение плагина целиком.

Отключать плагин целиком не стоит: девять его скилов по делу, из них
`urp-postprocessing` и `validate-urp-render-graph-renderer-feature` прямо по нашему
профилю.
