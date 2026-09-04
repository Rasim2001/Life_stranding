using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common;
using Common.SceneMarkers;
using Infastructure.Services.SaveLoadService;
using Infastructure.StaticData;
using Infastructure.StaticData.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Editor.World
{
    /// <summary>
    /// Проверка ключей памяти (<see cref="MarkerUniqueId"/>) по всем каталогам проекта:
    /// пустые ключи, дубликаты, читатели прогресса без <see cref="SceneProgressActor"/>,
    /// маркеры без компонента ключа, осиротевшие записи в GameData. Пространство ключей
    /// глобально по всем мирам (Р9 в .scratch/plans/jaunty-snuggling-bengio.md), поэтому область
    /// проверки — объединение сцен всех WorldCatalog в проекте, а не одного активного.
    /// Открывает сцены аддитивно и закрывает за собой — валидацию оверрайдов
    /// и вложенных префабов делает Unity, а не самописный парсер YAML.
    /// </summary>
    public class ActorKeysWindow : EditorWindow
    {
        private const string GameDataAssetPath = "Assets/Resources/StaticData/GameData/GameData.asset";

        private enum IssueKind
        {
            EmptyKey,
            DuplicateKey,
            MissingSceneProgressActor,
            MarkerWithoutKeyComponent,
            OrphanRecord,
            MarkerOutsideContentScenes
        }

        private class MarkerRecord
        {
            public string ScenePath;
            public string HierarchyPath;
            public string Key;
            public string GlobalObjectId;
            public bool IsLevelMarker;
            public bool IsContentScene;
        }

        private class Issue
        {
            public IssueKind Kind;
            public string Key;
            public List<MarkerRecord> Records = new List<MarkerRecord>();
        }

        private readonly List<Issue> _issues = new List<Issue>();
        private int _catalogCount;
        private int _sceneCount;
        private int _actorCount;
        private int _markerCount;
        private Vector2 _scroll;
        private string _statusMessage = "";

        [MenuItem("GD Tools/Validate/Actor Keys")]
        public static void Open() => GetWindow<ActorKeysWindow>("Actor Keys");

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Проверить", GUILayout.Width(100)))
                Validate();

            GUILayout.Label(
                $"Каталогов: {_catalogCount} · Сцен: {_sceneCount} · Акторов: {_actorCount} · " +
                $"Маркеров: {_markerCount} · Ошибок: {_issues.Count}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(_statusMessage))
                EditorGUILayout.HelpBox(_statusMessage, MessageType.Warning);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            if (_issues.Count == 0 && string.IsNullOrEmpty(_statusMessage) && _actorCount + _markerCount > 0)
                EditorGUILayout.LabelField("✓ Остальные ключи уникальны");

            foreach (Issue issue in _issues)
                DrawIssue(issue);

            EditorGUILayout.EndScrollView();
        }

        private void DrawIssue(Issue issue)
        {
            string title;
            switch (issue.Kind)
            {
                case IssueKind.EmptyKey:
                    title = "✗ Пустой ключ";
                    break;
                case IssueKind.DuplicateKey:
                    title = $"✗ Дубликат ключа  {issue.Key}";
                    break;
                case IssueKind.MissingSceneProgressActor:
                    title = "✗ Нет SceneProgressActor у читателя прогресса";
                    break;
                case IssueKind.MarkerWithoutKeyComponent:
                    title = "✗ Маркер без MarkerUniqueId";
                    break;
                case IssueKind.MarkerOutsideContentScenes:
                    title = "✗ Маркер уровня вне контентных сцен (в сбор не попадёт)";
                    break;
                default:
                    title = "✗ Осиротевшая запись в GameData (маркера в сценах каталога нет)";
                    break;
            }

            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

            bool canAssignKey = issue.Kind == IssueKind.EmptyKey || issue.Kind == IssueKind.DuplicateKey;

            foreach (MarkerRecord record in issue.Records)
            {
                EditorGUILayout.BeginHorizontal();

                string label = issue.Kind == IssueKind.OrphanRecord
                    ? $"  {record.Key} · {record.HierarchyPath}"
                    : $"  {Path.GetFileNameWithoutExtension(record.ScenePath)} · {record.HierarchyPath}";
                EditorGUILayout.LabelField(label);

                bool canShow = !string.IsNullOrEmpty(record.GlobalObjectId);

                if (canShow && GUILayout.Button("Показать", GUILayout.Width(80)))
                    Show(record);
                if (canAssignKey && canShow && GUILayout.Button("Новый ключ", GUILayout.Width(90)))
                    AssignNewKey(record);

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
        }

        private void Validate()
        {
            _issues.Clear();
            _statusMessage = "";
            _catalogCount = 0;
            _sceneCount = 0;
            _actorCount = 0;
            _markerCount = 0;

            List<WorldCatalog> catalogs = AssetDatabase.FindAssets("t:WorldCatalog")
                .Select(guid => AssetDatabase.LoadAssetAtPath<WorldCatalog>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(c => c != null)
                .ToList();

            if (catalogs.Count == 0)
            {
                _statusMessage = "В проекте нет ни одного WorldCatalog.";
                return;
            }

            var scenePaths = new List<string>();
            foreach (WorldCatalog catalog in catalogs)
                foreach (string path in CatalogBuildScenes.BuildDesiredPaths(catalog))
                    if (!scenePaths.Contains(path))
                        scenePaths.Add(path);

            var contentScenePaths = new HashSet<string>();
            foreach (WorldCatalog catalog in catalogs)
                foreach (string path in CatalogContentScenes.CollectScenePaths(catalog))
                    contentScenePaths.Add(path);

            if (scenePaths.Count == 0)
            {
                _statusMessage = "Ни в одном WorldCatalog не настроено ни одной сцены.";
                return;
            }

            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                if (EditorSceneManager.GetSceneAt(i).isDirty)
                {
                    _statusMessage = "Сохрани сцены и запусти снова.";
                    return;
                }
            }

            var keyRecords = new List<MarkerRecord>();
            var missingActorRecords = new List<MarkerRecord>();
            var missingKeyComponentRecords = new List<MarkerRecord>();
            SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                foreach (string path in scenePaths)
                {
                    Scene scene = EditorSceneManager.GetSceneByPath(path);
                    bool wasOpen = scene.IsValid() && scene.isLoaded;

                    if (!wasOpen)
                        scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

                    CollectMarkers(scene, contentScenePaths, keyRecords, missingActorRecords,
                        missingKeyComponentRecords);

                    if (!wasOpen)
                        EditorSceneManager.CloseScene(scene, removeScene: true);
                }
            }
            finally
            {
                if (setup != null && setup.Length > 0)
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
            }

            _catalogCount = catalogs.Count;
            _sceneCount = scenePaths.Count;
            _actorCount = keyRecords.Count(r => !r.IsLevelMarker);
            _markerCount = keyRecords.Count(r => r.IsLevelMarker);

            BuildIssues(keyRecords, missingActorRecords, missingKeyComponentRecords, catalogs);
        }

        private static void CollectMarkers(Scene scene, HashSet<string> contentScenePaths,
            List<MarkerRecord> keyRecords, List<MarkerRecord> missingActorRecords,
            List<MarkerRecord> missingKeyComponentRecords)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (MarkerUniqueId marker in root.GetComponentsInChildren<MarkerUniqueId>(true))
                {
                    var record = new MarkerRecord
                    {
                        ScenePath = scene.path,
                        HierarchyPath = CatalogContentScenes.GetHierarchyPath(marker.transform),
                        Key = marker.UniqueId,
                        GlobalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(marker.gameObject).ToString(),
                        IsLevelMarker = marker.GetComponent<MarkerBase>() != null,
                        IsContentScene = contentScenePaths.Contains(scene.path)
                    };
                    keyRecords.Add(record);

                    bool hasReader = marker.GetComponent<ISavedProgressReader>() != null;
                    bool hasSceneActor = marker.GetComponent<SceneProgressActor>() != null;
                    if (hasReader && !hasSceneActor)
                        missingActorRecords.Add(record);
                }

                foreach (MarkerBase levelMarker in root.GetComponentsInChildren<MarkerBase>(true))
                {
                    if (levelMarker.GetComponent<MarkerUniqueId>() != null)
                        continue;

                    missingKeyComponentRecords.Add(new MarkerRecord
                    {
                        ScenePath = scene.path,
                        HierarchyPath = CatalogContentScenes.GetHierarchyPath(levelMarker.transform),
                        GlobalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(levelMarker.gameObject).ToString()
                    });
                }
            }
        }

        private void BuildIssues(List<MarkerRecord> keyRecords, List<MarkerRecord> missingActorRecords,
            List<MarkerRecord> missingKeyComponentRecords, List<WorldCatalog> catalogs)
        {
            List<MarkerRecord> empty = keyRecords.Where(r => string.IsNullOrEmpty(r.Key)).ToList();
            if (empty.Count > 0)
                _issues.Add(new Issue { Kind = IssueKind.EmptyKey, Records = empty });

            var duplicateGroups = keyRecords
                .Where(r => !string.IsNullOrEmpty(r.Key))
                .GroupBy(r => r.Key)
                .Where(g => g.Count() > 1);

            foreach (var group in duplicateGroups)
                _issues.Add(new Issue { Kind = IssueKind.DuplicateKey, Key = group.Key, Records = group.ToList() });

            if (missingActorRecords.Count > 0)
                _issues.Add(new Issue
                {
                    Kind = IssueKind.MissingSceneProgressActor, Records = missingActorRecords
                });

            if (missingKeyComponentRecords.Count > 0)
                _issues.Add(new Issue
                {
                    Kind = IssueKind.MarkerWithoutKeyComponent, Records = missingKeyComponentRecords
                });

            List<MarkerRecord> outsideContent = keyRecords
                .Where(r => r.IsLevelMarker && !r.IsContentScene)
                .ToList();
            if (outsideContent.Count > 0)
                _issues.Add(new Issue { Kind = IssueKind.MarkerOutsideContentScenes, Records = outsideContent });

            List<MarkerRecord> orphans = FindOrphanRecords(catalogs, keyRecords);
            if (orphans.Count > 0)
                _issues.Add(new Issue { Kind = IssueKind.OrphanRecord, Records = orphans });
        }

        private static List<MarkerRecord> FindOrphanRecords(List<WorldCatalog> catalogs,
            List<MarkerRecord> keyRecords)
        {
            var orphans = new List<MarkerRecord>();

            var gameData = AssetDatabase.LoadAssetAtPath<GameStaticData>(GameDataAssetPath);
            if (gameData == null)
                return orphans;

            var presentKeys = new HashSet<string>(keyRecords
                .Where(r => r.IsLevelMarker && !string.IsNullOrEmpty(r.Key))
                .Select(r => r.Key));

            foreach (WorldCatalog catalog in catalogs)
            {
                if (string.IsNullOrEmpty(catalog.LevelDataKey))
                    continue;

                if (!gameData.GameDatas.TryGetValue(catalog.LevelDataKey, out GameData data))
                    continue;

                foreach (string key in GetRecordKeys(data))
                {
                    if (presentKeys.Contains(key))
                        continue;

                    orphans.Add(new MarkerRecord
                    {
                        ScenePath = "",
                        HierarchyPath = $"каталог {catalog.name}",
                        Key = key
                    });
                }
            }

            return orphans;
        }

        private static IEnumerable<string> GetRecordKeys(GameData data)
        {
            if (data == null)
                yield break;

            if (data.SpiderSpawnData != null && !string.IsNullOrEmpty(data.SpiderSpawnData.UniqueId))
                yield return data.SpiderSpawnData.UniqueId;

            if (data.FlowerSpawnData != null && !string.IsNullOrEmpty(data.FlowerSpawnData.UniqueId))
                yield return data.FlowerSpawnData.UniqueId;

            var lists = new[] { data.CheckPoints, data.GeneratorPoints, data.BatteriesPoints, data.EnergyPoints,
                data.ElephantPoints };

            foreach (List<WorldData> list in lists)
            {
                if (list == null)
                    continue;

                foreach (WorldData worldData in list)
                    if (worldData != null && !string.IsNullOrEmpty(worldData.UniqueId))
                        yield return worldData.UniqueId;
            }

            if (data.SkillsData == null)
                yield break;

            foreach (ProductSkillData skillData in data.SkillsData)
                if (skillData != null && !string.IsNullOrEmpty(skillData.UniqueId))
                    yield return skillData.UniqueId;
        }

        private void Show(MarkerRecord record)
        {
            GameObject go = ResolveGameObject(record);
            if (go == null)
                return;

            Selection.activeObject = go;
            EditorGUIUtility.PingObject(go);
        }

        private void AssignNewKey(MarkerRecord record)
        {
            GameObject go = ResolveGameObject(record);
            MarkerUniqueId marker = go != null ? go.GetComponent<MarkerUniqueId>() : null;
            if (marker == null)
                return;

            bool confirmed = EditorUtility.DisplayDialog(
                "Новый ключ",
                $"Память объекта \"{record.HierarchyPath}\" в сцене " +
                $"{Path.GetFileNameWithoutExtension(record.ScenePath)} будет сброшена. Продолжить?",
                "Выдать новый ключ",
                "Отмена");

            if (!confirmed)
                return;

            Undo.RecordObject(marker, "Assign new actor key");
            marker.UniqueId = Guid.NewGuid().ToString();
            PrefabUtility.RecordPrefabInstancePropertyModifications(marker);
            EditorUtility.SetDirty(marker);
            EditorSceneManager.MarkSceneDirty(marker.gameObject.scene);
        }

        private static GameObject ResolveGameObject(MarkerRecord record)
        {
            if (string.IsNullOrEmpty(record.GlobalObjectId))
                return null;

            if (!GlobalObjectId.TryParse(record.GlobalObjectId, out GlobalObjectId globalId))
                return null;

            Scene scene = EditorSceneManager.GetSceneByPath(record.ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
                EditorSceneManager.OpenScene(record.ScenePath, OpenSceneMode.Additive);

            return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalId) as GameObject;
        }
    }
}
