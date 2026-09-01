using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common;
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
    /// Проверка ключей памяти акторов (<see cref="MarkerUniqueId"/>) по всем сценам каталога:
    /// пустые ключи, дубликаты, читатели прогресса без <see cref="SceneProgressActor"/>.
    /// Открывает сцены каталога аддитивно и закрывает за собой — валидацию оверрайдов
    /// и вложенных префабов делает Unity, а не самописный парсер YAML.
    /// </summary>
    public class ActorKeysWindow : EditorWindow
    {
        private const string GameDataAssetPath = "Assets/Resources/StaticData/GameData/GameData.asset";

        private enum IssueKind
        {
            EmptyKey,
            DuplicateKey,
            MissingSceneProgressActor
        }

        private class MarkerRecord
        {
            public string ScenePath;
            public string HierarchyPath;
            public string Key;
            public string GlobalObjectId;
        }

        private class Issue
        {
            public IssueKind Kind;
            public string Key;
            public List<MarkerRecord> Records = new List<MarkerRecord>();
        }

        private readonly List<Issue> _issues = new List<Issue>();
        private int _sceneCount;
        private int _actorCount;
        private Vector2 _scroll;
        private string _statusMessage = "";

        [MenuItem("GD Tools/Validate/Actor Keys")]
        public static void Open() => GetWindow<ActorKeysWindow>("Actor Keys");

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Проверить", GUILayout.Width(100)))
                Validate();

            GUILayout.Label($"Сцен: {_sceneCount} · Акторов: {_actorCount} · Ошибок: {_issues.Count}",
                EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(_statusMessage))
                EditorGUILayout.HelpBox(_statusMessage, MessageType.Warning);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            if (_issues.Count == 0 && string.IsNullOrEmpty(_statusMessage) && _actorCount > 0)
                EditorGUILayout.LabelField("✓ Остальные ключи уникальны");

            foreach (Issue issue in _issues)
                DrawIssue(issue);

            EditorGUILayout.EndScrollView();
        }

        private void DrawIssue(Issue issue)
        {
            string title;
            if (issue.Kind == IssueKind.EmptyKey)
                title = "✗ Пустой ключ";
            else if (issue.Kind == IssueKind.DuplicateKey)
                title = $"✗ Дубликат ключа  {issue.Key}";
            else
                title = "✗ Нет SceneProgressActor у читателя прогресса";

            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

            foreach (MarkerRecord record in issue.Records)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(
                    $"  {Path.GetFileNameWithoutExtension(record.ScenePath)} · {record.HierarchyPath}");

                if (GUILayout.Button("Показать", GUILayout.Width(80)))
                    Show(record);
                if (GUILayout.Button("Новый ключ", GUILayout.Width(90)))
                    AssignNewKey(record);

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
        }

        private void Validate()
        {
            _issues.Clear();
            _statusMessage = "";
            _sceneCount = 0;
            _actorCount = 0;

            var gameData = AssetDatabase.LoadAssetAtPath<GameStaticData>(GameDataAssetPath);
            WorldCatalog catalog = gameData != null ? gameData.WorldCatalog : null;
            if (catalog == null)
            {
                _statusMessage = "GameData.asset не содержит WorldCatalog.";
                return;
            }

            List<string> scenePaths = CollectCatalogScenePaths(catalog);
            if (scenePaths.Count == 0)
            {
                _statusMessage = "В WorldCatalog не настроено ни одной сцены.";
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

            var records = new List<MarkerRecord>();
            var missingActorRecords = new List<MarkerRecord>();
            SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                foreach (string path in scenePaths)
                {
                    Scene scene = EditorSceneManager.GetSceneByPath(path);
                    bool wasOpen = scene.IsValid() && scene.isLoaded;

                    if (!wasOpen)
                        scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

                    CollectMarkers(scene, records, missingActorRecords);

                    if (!wasOpen)
                        EditorSceneManager.CloseScene(scene, removeScene: true);
                }
            }
            finally
            {
                if (setup != null && setup.Length > 0)
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
            }

            _sceneCount = scenePaths.Count;
            _actorCount = records.Count;
            BuildIssues(records, missingActorRecords);
        }

        private static List<string> CollectCatalogScenePaths(WorldCatalog catalog)
        {
            var paths = new List<string>();

            string entryPath = GetScenePath(catalog.EntryScene);
            if (!string.IsNullOrEmpty(entryPath))
                paths.Add(entryPath);

            foreach (SegmentDefinition segment in catalog.Segments)
            {
                if (segment == null)
                    continue;

                foreach (SceneReference reference in segment.SceneReferences)
                {
                    string path = GetScenePath(reference);
                    if (!string.IsNullOrEmpty(path) && !paths.Contains(path))
                        paths.Add(path);
                }
            }

            return paths;
        }

        private static string GetScenePath(SceneReference reference) =>
            reference != null && reference.SceneAsset != null
                ? AssetDatabase.GetAssetPath(reference.SceneAsset)
                : null;

        private static void CollectMarkers(Scene scene, List<MarkerRecord> records,
            List<MarkerRecord> missingActorRecords)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (MarkerUniqueId marker in root.GetComponentsInChildren<MarkerUniqueId>(true))
                {
                    var record = new MarkerRecord
                    {
                        ScenePath = scene.path,
                        HierarchyPath = GetHierarchyPath(marker.transform),
                        Key = marker.UniqueId,
                        GlobalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(marker).ToString()
                    };
                    records.Add(record);

                    bool hasReader = marker.GetComponent<ISavedProgressReader>() != null;
                    bool hasSceneActor = marker.GetComponent<SceneProgressActor>() != null;
                    if (hasReader && !hasSceneActor)
                        missingActorRecords.Add(record);
                }
            }
        }

        private static string GetHierarchyPath(Transform t)
        {
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }

            return path;
        }

        private void BuildIssues(List<MarkerRecord> records, List<MarkerRecord> missingActorRecords)
        {
            List<MarkerRecord> empty = records.Where(r => string.IsNullOrEmpty(r.Key)).ToList();
            if (empty.Count > 0)
                _issues.Add(new Issue { Kind = IssueKind.EmptyKey, Records = empty });

            var duplicateGroups = records
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
        }

        private void Show(MarkerRecord record)
        {
            MarkerUniqueId marker = ResolveMarker(record);
            if (marker == null)
                return;

            Selection.activeObject = marker.gameObject;
            EditorGUIUtility.PingObject(marker.gameObject);
        }

        private void AssignNewKey(MarkerRecord record)
        {
            MarkerUniqueId marker = ResolveMarker(record);
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

        private static MarkerUniqueId ResolveMarker(MarkerRecord record)
        {
            if (!GlobalObjectId.TryParse(record.GlobalObjectId, out GlobalObjectId globalId))
                return null;

            Scene scene = EditorSceneManager.GetSceneByPath(record.ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
                EditorSceneManager.OpenScene(record.ScenePath, OpenSceneMode.Additive);

            var go = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalId) as GameObject;
            return go != null ? go.GetComponent<MarkerUniqueId>() : null;
        }
    }
}
