# Регламент: два Unity MCP, кто чем владеет

**Статус:** действующий. Заведён 11.09.2026 по итогам живого сравнения обоих серверов.

В проекте подключены два MCP-сервера к одному редактору. Оба рабочие, оба проверены
вживую на Unity 6000.3.20f1. Они не конфликтуют сами по себе — конфликт возникает
только там, где в одно состояние пишут двое. Поэтому регламент раздаёт **владение
записью**, а чтение оставляет свободным.

## Серверы

| | `unity-mcp` (нативный) | `unityMCP` (MCP for Unity) |
|---|---|---|
| Откуда | пакет `com.unity.ai.assistant`, релей `~/.unity/relay/relay_win.exe` | пакет `com.coplaydev.unity-mcp` v10.2.0 + бридж |
| Тулов | 54, включаются галками в `Project Settings → AI → Unity MCP Server` | ~50, включаются группами (`manage_tools`) |
| Живёт | внутри редактора, версионируется с ним | отдельный мост, отдельная точка отказа |

Дальше по тексту — «нативный» и «Coplay».

## Таблица владения

Колонка «запись» не имеет пересечений. Это и есть гарантия от конфликта.

| Домен | Запись | Чтение |
|---|---|---|
| Партиклы, VFX Graph, Line/Trail | Coplay `manage_vfx` | Coplay |
| Материалы, текстуры | Coplay `manage_material`, `manage_texture` | Coplay |
| Префабы, компоненты, объекты внутри сцены | Coplay `manage_prefabs`, `manage_components`, `manage_gameobject` | Coplay |
| ScriptableObject | Coplay `manage_scriptable_object` | Coplay |
| Файл сцены: load / save / create | нативный `ManageScene` | нативный `ManageScene GetHierarchy` |
| Состояние редактора, Play Mode | нативный `ManageEditor` | нативный |
| Захват картинки | — | нативный `Camera_Capture` |
| Профайлер | — | нативный, 14 тулов |
| Правка AudioClip (trim, volume, loop) | нативный `AudioClip_Edit` | — |
| Сверка живого API | — | Coplay `unity_reflect` |
| Консоль | — | Coplay `read_console` |
| Произвольный C# | нативный `RunCommand` | Coplay `execute_code` |
| `.cs`, `.shader`, `.hlsl` | файловые тулы агента | файловые тулы агента |

Разрез по сцене читается так: **файл сцены — нативный, содержимое сцены — Coplay.**
Цели записи не пересекаются: один трогает `.unity` на диске, другой — объекты внутри
загруженной сцены.

Разрез по C# читается так: **пишем — нативным, читаем — Coplay.** У нативного
`RunCommand` настоящий Roslyn редактора и регистрация в Undo (`RegisterObjectCreation`,
`RegisterObjectModification`, `DestroyObject`). У Coplay `execute_code` — тело метода
с `return`, ответ приходит готовым JSON, но компилятор уходит в CodeDom (C# 6:
без интерполяции строк и кортежей; `var` и анонимные типы работают).

## Жёсткие правила

1. **Одну задачу не гнать на оба сервера.** Дублирование — это и есть конфликт.
2. В одном параллельном блоке вызовов — только чтения и только одного сервера.
   Любая мутация идёт отдельным вызовом.
3. `RunCommand` и `execute_code` не параллелить между собой никогда: обе компилируют
   код в редакторе.
4. Перед мутацией — `ManageEditor GetState`. Если `IsCompiling` или `IsUpdating` — ждать.
   При открытом Play Mode не компилировать вообще (см. `unity-verification-loop`).
5. Откат делать тем же сервером, которым правил: стеки Undo у них разные.
6. Скриншоты Coplay (`manage_camera screenshot`) — только с `output_folder` вне `Assets`.
   По умолчанию он пишет PNG в `Assets/Screenshots/` и тянет за собой импорт ассета.
   Для look-dev использовать нативный захват: чистый рендер 1920×1080 без гизмо
   и без записи на диск.

## Конфигурация

**Coplay — группы.** Нужны `core`, `vfx`, `docs`, `scripting_ext`. Выключены
`profiling` (владелец профайлера нативный), `testing` (своих тестов в проекте нет,
только вендорские Zenject), `ui`, `probuilder`, `animation`, `asset_gen`.

Состояние групп через `manage_tools` **живёт только в рамках сессии сервера**.
Постоянная настройка — галки в окне MCP for Unity в редакторе, оттуда состояние
подтягивается через `manage_tools sync`. Осторожно: `sync` перетирает сессионные
переключения состоянием редактора, поэтому вызывать его **до** `activate`/`deactivate`,
а не после.

**Нативный — галки по тулам.** Выключены девять, дублирующих домены Coplay:
`ManageGameObject`, `ManageAsset`, `ManageShader`, `ManageScript`,
`ManageScript_capabilities`, `CreateScript`, `DeleteScript`, `ApplyTextEdits`,
`ScriptApplyEdits`.

Оставлены: `RunCommand`, `ManageScene`, `ManageEditor`, `ManageMenuItem`, `ValidateScript`,
`GetSha`, `FindInFile`, `ReadConsole`, `GetConsoleLogs`, `Grep`, `Camera_Capture`,
`SceneView_Capture2DScene`, `SceneView_CaptureMultiAngleSceneView`, весь `Profiler_*`,
`AudioClip_Edit`, `GetProjectData`, `GetUserGuidelines`, `PackageManager_GetData`,
`PackageManager_ExecuteAction`, `ListResources`, `ReadResource`, `ImportExternalModel`.

**Состояние на 11.09.2026, проверено:** нативный — 45 из 54; Coplay — 24 из 35,
группы `core`, `docs`, `scripting_ext`, `vfx`, синхронизировано с редактором
через `manage_tools sync`.

### Генерация выключена сознательно

Владелец не генерирует ассеты — работа идёт с уже имеющимися звуком, музыкой и текстурами.
У Coplay группа `asset_gen` выключена. Нативный `AssetGeneration_*` (12 тулов) технически
включён, но мёртв: `GetModels` возвращает пустой список. Его можно выключить для чистоты,
на поведение это не влияет.

Исключение — `PackageManager_ExecuteAction`: это тул **на запись** в манифест пакетов.
Пользоваться им только по прямой просьбе владельца.

## Рабочее соглашение по FX и шейдерам

Договорённость с владельцем от 11.09.2026.

**Шейдеры пишутся текстом.** Только `.shader` и `.hlsl` в `Assets/Art/Shaders/`,
файловыми тулами агента. `manage_shader` у обоих серверов не использовать: это CRUD
по тексту файла без единого преимущества перед прямой правкой. Проверка после правки —
скриптом через `ShaderUtil.GetShaderMessages`, правила авторинга — скил
`urp-shader-authoring` и `docs/lighting-and-shading.md`.

**VFX агент собирает базу и объясняет её.** На задачу по эффекту:
1. создать основу — `particle_create` или `vfx_create_asset` из шаблона, назначить,
   выставить модули и параметры;
2. **разобрать вслух, что за что отвечает** — какой модуль даёт форму, какой движение,
   какой затухание, где крутить, чтобы получить нужное. Это не опция, а часть задачи:
   дальше эффект доводит владелец руками.

**Текстуры под FX делает владелец.** Агент выдаёт техническое задание: разрешение,
каналы и что в каждом лежит, цветовое пространство (sRGB или linear), тайлинг, альфа,
формат сжатия. Готовый файл владелец кладёт в проект сам. Настройки импорта агент
может проставить через Coplay `manage_texture`.

Границу помнить: узлы VFX Graph и Shader Graph не собирает никто. Агент создаёт ассет
из шаблона, назначает и крутит экспонированные параметры — сам граф собирает владелец.

## Что не покрыто ни одним сервером

- **Авторинг графов.** Ни VFX Graph, ни Shader Graph узлами не собираются. Coplay умеет
  создать ассет из шаблона, назначить его и крутить экспонированные параметры — не более.
- **Шейдерный код.** `manage_shader` у обоих — это CRUD по тексту файла. Диагностика
  компиляции достаётся скриптом через `ShaderUtil.GetShaderMessages`.
- **Генерация чего угодно.** Не используется по решению владельца: работаем с уже
  имеющимися звуком, музыкой и текстурами. Соответствующие группы выключены.
- **AudioSource и микшер.** Отдельного тула нет ни у кого; через `manage_components`
  или `RunCommand`, плюс скил `audio-setup-mixers` из плагина Unity.

## Замеры, на которых построено решение

Сделаны 11.09.2026 на сцене `Seg_01_Base`.

- `MT_Lit_TEST.mat`: Coplay `get_material_info` вернул шейдер и 50 свойств со значениями;
  нативный `ManageAsset GetInfo` — только guid, тип и дату, свойств нет.
- `ProbeReferenceVolume`: Coplay `unity_reflect` за один вызов выдал `lightingScenario`,
  `otherScenario`, `BlendLightingScenario`, `scenarioBlendingFactor` и пометил
  `RenderDebug` как obsolete. У нативного аналога нет.
- Захват: нативный — чистый рендер 1920×1080, ничего не пишет на диск; Coplay — снимок
  вьюпорта с гизмо и оверлеями, PNG в `Assets/Screenshots/`.
- Иерархия: нативный `GetHierarchy` — всё дерево одним вызовом с `activeSelf`,
  `activeInHierarchy`, `isStatic`, слоем и ID; Coplay `find_gameobjects` — только
  массив instanceID, детали отдельными вызовами.
- FX: у Coplay 18 действий по ParticleSystem и 25 по VFX Graph; у нативного по FX
  нет ничего.
- `ENV_Lit.shader` через оба: 0 сообщений, 5 пассов, 28 свойств, очередь 2000.
