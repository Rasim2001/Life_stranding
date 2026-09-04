using System.Collections.Generic;
using System.IO;
using System.Linq;
using Infastructure.StaticData.World;
using Infastructure.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Editor.World
{
    /// <summary>
    /// Проверка структуры сцен по всем каталогам проекта (docs/scene-regulations.md §4, §5,
    /// §6, §9, §11): живая ветка сохранена выключенной и не содержит того, что не должна;
    /// группировочные объекты и корни сцен сегментов несут единичный трансформ; Probe Volume
    /// не приезжает в префаб и не приезжает в резидентный слой/атмосферу/бутстрап; звук
    /// сегмента не 2D и не бьёт дальше разумного. Открывает сцены аддитивно и закрывает
    /// за собой, как ActorKeysWindow — валидацию оверрайдов и вложенных префабов делает
    /// Unity, а не самописный парсер YAML.
    /// </summary>
    public class SceneStructureWindow : EditorWindow
    {
        private const float MaxAudioDistance = 100f;
        private const float ScaleTolerance = 1e-6f;

        private enum SceneRole
        {
            Bootstrap,
            Resident,
            Atmosphere,
            Entry,
            Segment
        }

        private enum Level
        {
            Error,
            Warning
        }

        private class Finding
        {
            public Level Level;
            public string Message;
            public string ScenePath;
            public string HierarchyPath;
            public string GlobalObjectId;
        }

        private readonly List<Finding> _findings = new List<Finding>();
        private int _catalogCount;
        private int _sceneCount;
        private Vector2 _scroll;
        private string _statusMessage = "";

        [MenuItem("GD Tools/Validate/Scene Structure")]
        public static void Open() => GetWindow<SceneStructureWindow>("Scene Structure");

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Проверить", GUILayout.Width(100)))
                Validate();

            int errors = _findings.Count(f => f.Level == Level.Error);
            int warnings = _findings.Count(f => f.Level == Level.Warning);
            GUILayout.Label($"Каталогов: {_catalogCount} · Сцен: {_sceneCount} · " +
                $"Ошибок: {errors} · Предупреждений: {warnings}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(_statusMessage))
                EditorGUILayout.HelpBox(_statusMessage, MessageType.Warning);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            if (_findings.Count == 0 && string.IsNullOrEmpty(_statusMessage) && _sceneCount > 0)
                EditorGUILayout.LabelField("✓ Структура в порядке");

            foreach (Finding finding in _findings)
                DrawFinding(finding);

            EditorGUILayout.EndScrollView();
        }

        private void DrawFinding(Finding finding)
        {
            EditorGUILayout.BeginHorizontal();

            string mark = finding.Level == Level.Error ? "✗" : "⚠";
            string scene = string.IsNullOrEmpty(finding.ScenePath)
                ? ""
                : Path.GetFileNameWithoutExtension(finding.ScenePath) + " · ";
            EditorGUILayout.LabelField($"{mark} {finding.Message} — {scene}{finding.HierarchyPath}");

            if (!string.IsNullOrEmpty(finding.GlobalObjectId) &&
                GUILayout.Button("Показать", GUILayout.Width(80)))
                Show(finding);

            EditorGUILayout.EndHorizontal();
        }

        private void Validate()
        {
            _findings.Clear();
            _statusMessage = "";
            _catalogCount = 0;
            _sceneCount = 0;

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
            var roleByPath = new Dictionary<string, SceneRole>();

            foreach (WorldCatalog catalog in catalogs)
                CollectScenePaths(catalog, scenePaths, roleByPath);

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

            SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                foreach (string path in scenePaths)
                {
                    Scene scene = EditorSceneManager.GetSceneByPath(path);
                    bool wasOpen = scene.IsValid() && scene.isLoaded;

                    if (!wasOpen)
                        scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

                    ValidateScene(scene, roleByPath[path]);

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
        }

        private static void CollectScenePaths(WorldCatalog catalog, List<string> scenePaths,
            Dictionary<string, SceneRole> roleByPath)
        {
            void Add(SceneReference reference, SceneRole role)
            {
                string path = CatalogBuildScenes.GetScenePath(reference);
                if (string.IsNullOrEmpty(path))
                    return;

                if (!scenePaths.Contains(path))
                    scenePaths.Add(path);

                if (!roleByPath.ContainsKey(path))
                    roleByPath[path] = role;
            }

            Add(catalog.BootstrapScene, SceneRole.Bootstrap);

            if (catalog.ResidentScenes != null)
                foreach (SceneReference reference in catalog.ResidentScenes)
                    Add(reference, SceneRole.Resident);

            Add(catalog.AtmosphereScene, SceneRole.Atmosphere);
            Add(catalog.EntryScene, SceneRole.Entry);

            foreach (SegmentDefinition segment in catalog.Segments)
            {
                if (segment == null)
                    continue;

                foreach (SceneReference reference in segment.SceneReferences)
                    Add(reference, SceneRole.Segment);
            }
        }

        private void ValidateScene(Scene scene, SceneRole role)
        {
            GameObject[] roots = scene.GetRootGameObjects();

            ValidateLiveRoots(scene, roots);
            ValidateGroups(scene, roots, role);
            ValidateProbeVolumes(scene, roots, role);

            if (role == SceneRole.Segment)
                ValidateSegmentAudio(scene, roots);
        }

        // --- Живая ветка (docs/scene-regulations.md §6) ---

        private void ValidateLiveRoots(Scene scene, GameObject[] roots)
        {
            var liveRoots = new List<SegmentLiveRoot>();
            foreach (GameObject root in roots)
                liveRoots.AddRange(root.GetComponentsInChildren<SegmentLiveRoot>(true));

            foreach (SegmentLiveRoot liveRoot in liveRoots)
            {
                if (liveRoot.gameObject.activeSelf)
                    AddFinding(Level.Error, "Live-ветка сохранена включённой", scene, liveRoot.transform);

                foreach (Collider collider in liveRoot.GetComponentsInChildren<Collider>(true))
                    if (!collider.isTrigger)
                        AddFinding(Level.Error, "не-триггерный Collider под Live-веткой",
                            scene, collider.transform);

                foreach (SegmentInjector injector in liveRoot.GetComponentsInChildren<SegmentInjector>(true))
                    AddFinding(Level.Error, "SegmentInjector внутри Live-ветки", scene, injector.transform);
            }

            foreach (GameObject root in roots)
            {
                foreach (AudioSource audioSource in root.GetComponentsInChildren<AudioSource>(true))
                    if (audioSource.playOnAwake && !IsUnderLiveRoot(audioSource.transform))
                        AddFinding(Level.Error, "AudioSource.playOnAwake вне Live-ветки",
                            scene, audioSource.transform);

                foreach (ParticleSystem particleSystem in root.GetComponentsInChildren<ParticleSystem>(true))
                    if (particleSystem.main.playOnAwake && !IsUnderLiveRoot(particleSystem.transform))
                        AddFinding(Level.Error, "ParticleSystem.playOnAwake вне Live-ветки",
                            scene, particleSystem.transform);
            }
        }

        private static bool IsUnderLiveRoot(Transform t)
        {
            for (Transform p = t; p != null; p = p.parent)
                if (p.GetComponent<SegmentLiveRoot>() != null)
                    return true;

            return false;
        }

        // --- Группы и корни сцен сегментов (docs/scene-regulations.md §4) ---

        private void ValidateGroups(Scene scene, GameObject[] roots, SceneRole role)
        {
            foreach (GameObject root in roots)
            {
                if (role == SceneRole.Segment)
                {
                    Transform t = root.transform;
                    bool identity = t.localPosition == Vector3.zero &&
                        t.localRotation == Quaternion.identity &&
                        (t.localScale - Vector3.one).sqrMagnitude <= ScaleTolerance;

                    if (!identity)
                        AddFinding(Level.Error, "корень сцены сегмента не единичный трансформ", scene, t);

                    foreach (Transform child in root.transform)
                        WalkGroups(child.gameObject, scene);

                    continue;
                }

                WalkGroups(root, scene);
            }
        }

        private void WalkGroups(GameObject go, Scene scene)
        {
            bool isGroup = go.transform.childCount > 0 &&
                go.GetComponent<Renderer>() == null &&
                go.GetComponent<Collider>() == null &&
                go.GetComponent<MeshFilter>() == null;

            bool scaled = (go.transform.localScale - Vector3.one).sqrMagnitude > ScaleTolerance;

            // Риск, под который писалось правило (§4) — перекошенные нормали Collider'ов
            // под неединичным scale. Группа без единого Collider в поддереве этого риска
            // не несёт (пример: MOV_Pod_* — обёртка со скриптом вокруг чистых визуальных
            // маркеров без коллизии) — гейт по факту риска, а не по наличию компонента,
            // чтобы не открыть слепую зону на wrapper-акторах с реальными коллайдерами
            // (DestoyableObject, Bridge, Generator — §5).
            if (isGroup && scaled && go.GetComponentsInChildren<Collider>(true).Length > 0)
                AddFinding(Level.Error, "группировочный объект с неединичным scale", scene, go.transform);

            if (go.GetComponent<Animator>() != null || go.GetComponent<SkinnedMeshRenderer>() != null)
                return;

            foreach (Transform child in go.transform)
                WalkGroups(child.gameObject, scene);
        }

        // --- Probe Volume (docs/scene-regulations.md §2, §11) ---

        private void ValidateProbeVolumes(Scene scene, GameObject[] roots, SceneRole role)
        {
            bool forbidsAny = role == SceneRole.Resident || role == SceneRole.Atmosphere ||
                role == SceneRole.Bootstrap;

            foreach (GameObject root in roots)
            {
                foreach (ProbeVolume probeVolume in root.GetComponentsInChildren<ProbeVolume>(true))
                {
                    if (PrefabUtility.IsPartOfPrefabInstance(probeVolume.gameObject))
                        AddFinding(Level.Error, "Probe Volume внутри инстанса префаба",
                            scene, probeVolume.transform);

                    if (forbidsAny)
                        AddFinding(Level.Error, "Probe Volume в резидентной сцене/атмосфере/бутстрапе",
                            scene, probeVolume.transform);
                }
            }
        }

        // --- Звук сегмента (docs/scene-regulations.md §9) ---

        private void ValidateSegmentAudio(Scene scene, GameObject[] roots)
        {
            foreach (GameObject root in roots)
            {
                foreach (AudioSource audioSource in root.GetComponentsInChildren<AudioSource>(true))
                {
                    if (audioSource.spatialBlend == 0f)
                        AddFinding(Level.Error, "2D-звук (spatialBlend 0) в сцене сегмента",
                            scene, audioSource.transform);

                    if (audioSource.maxDistance > MaxAudioDistance)
                        AddFinding(Level.Warning,
                            $"maxDistance {audioSource.maxDistance:0} > {MaxAudioDistance:0}",
                            scene, audioSource.transform);
                }
            }
        }

        private void AddFinding(Level level, string message, Scene scene, Transform t)
        {
            string hierarchyPath = CatalogContentScenes.GetHierarchyPath(t);

            _findings.Add(new Finding
            {
                Level = level,
                Message = message,
                ScenePath = scene.path,
                HierarchyPath = hierarchyPath,
                GlobalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(t.gameObject).ToString()
            });

            string logMessage = $"[SceneStructure] {message} — " +
                $"{Path.GetFileNameWithoutExtension(scene.path)} · {hierarchyPath}";

            if (level == Level.Error)
                Debug.LogError(logMessage, t.gameObject);
            else
                Debug.LogWarning(logMessage, t.gameObject);
        }

        private void Show(Finding finding)
        {
            GameObject go = ResolveGameObject(finding.GlobalObjectId, finding.ScenePath);
            if (go == null)
                return;

            Selection.activeObject = go;
            EditorGUIUtility.PingObject(go);
        }

        private static GameObject ResolveGameObject(string globalObjectId, string scenePath)
        {
            if (string.IsNullOrEmpty(globalObjectId))
                return null;

            if (!GlobalObjectId.TryParse(globalObjectId, out GlobalObjectId globalId))
                return null;

            Scene scene = EditorSceneManager.GetSceneByPath(scenePath);
            if (!scene.IsValid() || !scene.isLoaded)
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

            return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalId) as GameObject;
        }
    }
}
