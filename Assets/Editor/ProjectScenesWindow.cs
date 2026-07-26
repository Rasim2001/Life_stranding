using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Infastructure.StaticData;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Editor
{
    /// <summary>
    /// Overview of project scenes: where logic lives (SceneContext) vs pure content,
    /// how many markers, whether there's a GameDatas entry, Build Settings and config state.
    /// Switching the main/additive scenes and launching Play from any scene.
    /// Scene content is determined by text-parsing .unity (YAML), without opening scenes.
    /// </summary>
    public class ProjectScenesWindow : EditorWindow
    {
        private const string ScenesRoot = "Assets/Scenes/";
        private const string BootstrapScenePath = "Assets/Scenes/Bootstrap.unity";
        private const string GameDataAssetPath = "Assets/Resources/StaticData/GameData/GameData.asset";
        private const string SceneContextScriptPath = "Assets/Plugins/Zenject/Source/Install/Contexts/SceneContext.cs";

        private static readonly (string Label, string ScriptPath)[] MarkerTypes =
        {
            ("Spider", "Assets/Scripts/Common/SceneMarkers/SpiderSpawnPointMarker.cs"),
            ("Flower", "Assets/Scripts/Common/SceneMarkers/FlowerPointMarker.cs"),
            ("Checkpoint", "Assets/Scripts/Common/SceneMarkers/CheckPointMarker.cs"),
            ("Battery", "Assets/Scripts/Common/SceneMarkers/BatteryPointMarker.cs"),
            ("Energy", "Assets/Scripts/Common/SceneMarkers/EnergyPointMarker.cs"),
            ("Elephant", "Assets/Scripts/Common/SceneMarkers/ElephantPointMarker.cs"),
            ("Generator", "Assets/Scripts/Common/SceneMarkers/GeneratorPointMarker.cs"),
            ("Skill", "Assets/Scripts/Common/SceneMarkers/ProductSkillPointMarker.cs"),
        };

        // Scenes with a special role in the boot/exit-loop pipeline. Setting them as the
        // main or an additive scene doesn't crash, but silently breaks the game (empty
        // level with no SceneInstaller bindings, or a broken defeat/pause/win exit flow).
        private static readonly Dictionary<string, string> UtilitySceneReasons = new Dictionary<string, string>
        {
            ["Bootstrap"] =
                "Entry-point scene: bare SceneContext, no SceneInstaller. " +
                "Setting it as Main/Additive produces an empty level — world/UI bindings never run.",
            ["ExitGameLoop"] =
                "Transitional unload scene used internally by ExitGameLoopState " +
                "(defeat/pause/win flow) — not a content scene.",
        };

        private readonly List<SceneRow> _rows = new List<SceneRow>();
        private readonly Dictionary<string, string> _scenePathByName = new Dictionary<string, string>();
        private GameStaticData _gameData;
        private Vector2 _scroll;
        private string _lastRefreshInfo = "";

        private class SceneRow
        {
            public string Name;
            public string Path;
            public bool HasSceneContext;
            public int[] MarkerCounts;
            public bool HasGameDataEntry;
            public int GameDataPointsCount;
            public string BuildSettingsState;
            public int ApproxObjectCount;
            public string ConfigRole;
            public string UtilityReason;
        }

        [MenuItem("SpiderRig/Scenes")]
        public static void Open()
        {
            var window = GetWindow<ProjectScenesWindow>("Project Scenes");
            window.Refresh();
        }

        private void OnEnable() => Refresh();

        private void Refresh()
        {
            _rows.Clear();
            _scenePathByName.Clear();

            string sceneContextGuid = AssetDatabase.AssetPathToGUID(SceneContextScriptPath);
            string[] markerGuids = MarkerTypes.Select(m => AssetDatabase.AssetPathToGUID(m.ScriptPath)).ToArray();

            _gameData = AssetDatabase.LoadAssetAtPath<GameStaticData>(GameDataAssetPath);
            GameStaticData gameData = _gameData;

            List<string> scenePaths = AssetDatabase.FindAssets("t:Scene")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p.StartsWith(ScenesRoot, StringComparison.OrdinalIgnoreCase))
                .Distinct()
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (string path in scenePaths)
            {
                string text;
                try
                {
                    text = File.ReadAllText(path);
                }
                catch (IOException)
                {
                    continue;
                }

                var row = new SceneRow
                {
                    Name = Path.GetFileNameWithoutExtension(path),
                    Path = path,
                    HasSceneContext = !string.IsNullOrEmpty(sceneContextGuid) && text.Contains(sceneContextGuid),
                    MarkerCounts = markerGuids
                        .Select(guid => string.IsNullOrEmpty(guid) ? 0 : CountOccurrences(text, guid))
                        .ToArray(),
                    ApproxObjectCount = CountOccurrences(text, "\n--- !u!1 &"),
                    BuildSettingsState = GetBuildSettingsState(path),
                    ConfigRole = "—",
                };

                UtilitySceneReasons.TryGetValue(row.Name, out row.UtilityReason);

                if (gameData != null)
                {
                    if (gameData.GameDatas != null && gameData.GameDatas.TryGetValue(row.Name, out GameData gd))
                    {
                        row.HasGameDataEntry = true;
                        row.GameDataPointsCount = CountGameDataPoints(gd);
                    }

                    if (string.Equals(gameData.LoadScene, row.Name, StringComparison.Ordinal))
                        row.ConfigRole = "Main";
                    else if (gameData.AdditiveScenes != null && gameData.AdditiveScenes.Contains(row.Name))
                        row.ConfigRole = "Additive";
                }

                _scenePathByName[row.Name] = path;
                _rows.Add(row);
            }

            // Group rows top to bottom: Utility scenes first (they're noise, not levels),
            // then Logic scenes (candidate Main), then Content scenes (candidate Additive).
            // Within a group: current Main first, then Additive, then Build Settings-enabled,
            // then alphabetical. Renaming scenes into real name-based groups is a separate
            // task (see docs/asset-organization-and-naming.md §6.6) — this is just a stopgap.
            List<SceneRow> sorted = _rows
                .OrderBy(r => GetGroupIndex(r))
                .ThenByDescending(r => r.ConfigRole == "Main")
                .ThenByDescending(r => r.ConfigRole == "Additive")
                .ThenByDescending(r => r.BuildSettingsState == "enabled")
                .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            _rows.Clear();
            _rows.AddRange(sorted);

            _lastRefreshInfo = $"Scenes: {_rows.Count} · refreshed {DateTime.Now:HH:mm:ss}";
        }

        private void SetAsMain(SceneRow row)
        {
            if (_gameData == null)
                return;

            if (row.UtilityReason != null)
            {
                EditorUtility.DisplayDialog("Utility scene",
                    $"«{row.Name}» is a utility scene, not a level: {row.UtilityReason}",
                    "Got it");
                return;
            }

            if (!row.HasSceneContext)
            {
                EditorUtility.DisplayDialog("Can't set as Main",
                    $"Scene «{row.Name}» has no SceneContext — SceneInstaller and BuildLevelState won't run, the spider won't appear.",
                    "Got it");
                return;
            }

            if (_gameData.AdditiveScenes != null && _gameData.AdditiveScenes.Contains(row.Name))
            {
                EditorUtility.DisplayDialog("Can't set as Main",
                    $"Scene «{row.Name}» is already in the Additive list — Unity would load a second copy of the geometry. Remove it from Additive first.",
                    "Got it");
                return;
            }

            if (!row.HasGameDataEntry)
            {
                bool proceed = EditorUtility.DisplayDialog("No GameDatas entry",
                    $"Scene «{row.Name}» has no entry in GameDatas — the runtime will throw KeyNotFoundException in GameFactory. Set LoadScene anyway?",
                    "Set anyway", "Cancel");
                if (!proceed)
                    return;
            }

            Undo.RecordObject(_gameData, "Set LoadScene");
            _gameData.LoadScene = row.Name;
            EditorUtility.SetDirty(_gameData);
            AssetDatabase.SaveAssets();

            Refresh();
        }

        private void AddToAdditive(SceneRow row)
        {
            if (_gameData == null)
                return;

            if (row.UtilityReason != null)
            {
                EditorUtility.DisplayDialog("Utility scene",
                    $"«{row.Name}» is a utility scene, not a level: {row.UtilityReason}",
                    "Got it");
                return;
            }

            if (string.Equals(_gameData.LoadScene, row.Name, StringComparison.Ordinal))
            {
                EditorUtility.DisplayDialog("Can't add to Additive",
                    $"Scene «{row.Name}» is already Main — Unity would load a second copy of the geometry.",
                    "Got it");
                return;
            }

            List<string> list = (_gameData.AdditiveScenes ?? Array.Empty<string>()).ToList();
            if (list.Contains(row.Name))
                return;

            list.Add(row.Name);

            Undo.RecordObject(_gameData, "Add to AdditiveScenes");
            _gameData.AdditiveScenes = list.ToArray();
            EditorUtility.SetDirty(_gameData);
            AssetDatabase.SaveAssets();

            Refresh();
        }

        private void RemoveFromAdditive(SceneRow row)
        {
            if (_gameData == null || _gameData.AdditiveScenes == null)
                return;

            List<string> list = _gameData.AdditiveScenes.ToList();
            if (!list.Remove(row.Name))
                return;

            Undo.RecordObject(_gameData, "Remove from AdditiveScenes");
            _gameData.AdditiveScenes = list.ToArray();
            EditorUtility.SetDirty(_gameData);
            AssetDatabase.SaveAssets();

            Refresh();
        }

        private void OpenConfiguredSet()
        {
            if (_gameData == null || string.IsNullOrEmpty(_gameData.LoadScene))
                return;

            if (!_scenePathByName.TryGetValue(_gameData.LoadScene, out string mainPath))
            {
                EditorUtility.DisplayDialog("Not found",
                    $"Main scene file «{_gameData.LoadScene}» was not found among scanned scenes.",
                    "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EditorSceneManager.OpenScene(mainPath, OpenSceneMode.Single);

            foreach (string additiveName in _gameData.AdditiveScenes ?? Array.Empty<string>())
            {
                if (_scenePathByName.TryGetValue(additiveName, out string additivePath))
                    EditorSceneManager.OpenScene(additivePath, OpenSceneMode.Additive);
            }
        }

        private void Play()
        {
            if (!TrySetPlayModeStartScene())
                return;

            EditorApplication.isPlaying = true;
        }

        private void PlayThisScene()
        {
            string sceneName = EditorSceneManager.GetActiveScene().name;

            Refresh();

            SceneRow row = _rows.FirstOrDefault(r => r.Name == sceneName);
            if (row == null)
            {
                EditorUtility.DisplayDialog("Scene not found",
                    $"Open scene «{sceneName}» was not found among scenes in {ScenesRoot}.",
                    "OK");
                return;
            }

            SetAsMain(row);

            if (_gameData == null || !string.Equals(_gameData.LoadScene, sceneName, StringComparison.Ordinal))
                return;

            Play();
        }

        private bool TrySetPlayModeStartScene()
        {
            var bootstrapAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapScenePath);
            if (bootstrapAsset == null)
            {
                EditorUtility.DisplayDialog("Bootstrap not found",
                    $"Scene file not found: {BootstrapScenePath}",
                    "OK");
                return false;
            }

            EditorSceneManager.playModeStartScene = bootstrapAsset;
            return true;
        }

        private static void ResetPlayModeStartScene() =>
            EditorSceneManager.playModeStartScene = null;

        private static int CountGameDataPoints(GameData gd)
        {
            int count = 0;
            count += gd.SpiderSpawnData != null ? 1 : 0;
            count += gd.FlowerSpawnData != null ? 1 : 0;
            count += gd.CheckPoints?.Count ?? 0;
            count += gd.GeneratorPoints?.Count ?? 0;
            count += gd.BatteriesPoints?.Count ?? 0;
            count += gd.EnergyPoints?.Count ?? 0;
            count += gd.ElephantPoints?.Count ?? 0;
            count += gd.SkillsData?.Count ?? 0;
            return count;
        }

        private static string GetBuildSettingsState(string path)
        {
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.path == path)
                    return scene.enabled ? "enabled" : "disabled";
            }

            return "absent";
        }

        private static int CountOccurrences(string haystack, string needle)
        {
            int count = 0;
            int index = 0;

            while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) != -1)
            {
                count++;
                index += needle.Length;
            }

            return count;
        }

        private string GetConfiguredSetSummary()
        {
            if (_gameData == null || string.IsNullOrEmpty(_gameData.LoadScene))
                return "(LoadScene not set)";

            string additive = _gameData.AdditiveScenes != null && _gameData.AdditiveScenes.Length > 0
                ? " + " + string.Join(" + ", _gameData.AdditiveScenes)
                : "";

            return $"{_gameData.LoadScene}{additive}";
        }

        private static int GetGroupIndex(SceneRow row)
        {
            if (row.UtilityReason != null)
                return 0;

            return row.HasSceneContext ? 1 : 2;
        }

        private static string GetGroupLabel(int group)
        {
            if (group == 0)
                return "Utility scenes";

            return group == 1
                ? "Logic scenes (candidate Main)"
                : "Content scenes (candidate Additive)";
        }

        private static GUIStyle _subLabelStyle;

        private static GUIStyle SubLabelStyle
        {
            get
            {
                if (_subLabelStyle == null)
                    _subLabelStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel) { wordWrap = true };
                return _subLabelStyle;
            }
        }

        private static void DrawActionButton(string label, string subLabel, string tooltip, float width, Action onClick)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(width));

            if (GUILayout.Button(new GUIContent(label, tooltip), GUILayout.Width(width)))
                onClick();

            GUILayout.Label(subLabel, SubLabelStyle, GUILayout.Width(width));

            EditorGUILayout.EndVertical();
        }

        private static void DrawGroupSeparator(string label)
        {
            EditorGUILayout.Space(6);
            GUILayout.Label(label, EditorStyles.boldLabel);
            Rect rect = GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.6f));
            EditorGUILayout.Space(2);
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(new GUIContent("Refresh", "Re-scan all project scenes and rebuild this table."), GUILayout.Width(100)))
                Refresh();

            GUILayout.Label(_lastRefreshInfo, EditorStyles.miniLabel);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();

            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                DrawActionButton("Play", GetConfiguredSetSummary(),
                    "Enter Play mode via Bootstrap, which then loads the scene set shown below (LoadScene + AdditiveScenes).",
                    140, Play);

                string activeSceneName = EditorSceneManager.GetActiveScene().name;
                DrawActionButton("Play This Scene", activeSceneName,
                    "Set the currently open scene as LoadScene (same validation as 'Set as Main'), then enter Play mode via Bootstrap.",
                    140, PlayThisScene);

                DrawActionButton("Open Set", GetConfiguredSetSummary(),
                    "Open the currently configured Main scene (Single) plus all Additive scenes in the editor, without entering Play mode.",
                    140, OpenConfiguredSet);
            }

            SceneAsset startScene = EditorSceneManager.playModeStartScene;
            DrawActionButton("Reset Start Scene", startScene != null ? startScene.name : "default (not set)",
                "Clear playModeStartScene so normal Unity Play behavior (start from the currently open scene) is restored.",
                140, ResetPlayModeStartScene);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            DrawHeader();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            int lastGroup = -1;
            foreach (SceneRow row in _rows)
            {
                int group = GetGroupIndex(row);
                if (group != lastGroup)
                {
                    DrawGroupSeparator(GetGroupLabel(group));
                    lastGroup = group;
                }

                DrawRow(row);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Name & Path", EditorStyles.boldLabel, GUILayout.Width(260));
            GUILayout.Label("Role", EditorStyles.boldLabel, GUILayout.Width(70));
            GUILayout.Label("Markers", EditorStyles.boldLabel, GUILayout.Width(320));
            GUILayout.Label("GameDatas", EditorStyles.boldLabel, GUILayout.Width(140));
            GUILayout.Label("Build Settings", EditorStyles.boldLabel, GUILayout.Width(90));
            GUILayout.Label("Objects", EditorStyles.boldLabel, GUILayout.Width(70));
            GUILayout.Label("Config Role", EditorStyles.boldLabel, GUILayout.Width(80));
            GUILayout.Label("Actions", EditorStyles.boldLabel, GUILayout.Width(240));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawRow(SceneRow row)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(GUILayout.Width(260));
            GUILayout.Label(row.Name, EditorStyles.label);
            GUILayout.Label(row.Path, EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            GUILayout.Label(row.HasSceneContext ? "logic" : "content", GUILayout.Width(70));

            string markers = string.Join("  ", MarkerTypes
                .Select((m, i) => $"{m.Label}:{row.MarkerCounts[i]}"));
            GUILayout.Label(markers, EditorStyles.wordWrappedLabel, GUILayout.Width(320));

            string gameDatas = row.HasGameDataEntry
                ? $"yes, points: {row.GameDataPointsCount}"
                : "no";
            GUILayout.Label(gameDatas, GUILayout.Width(140));

            GUILayout.Label(row.BuildSettingsState, GUILayout.Width(90));
            GUILayout.Label(row.ApproxObjectCount.ToString(), GUILayout.Width(70));
            GUILayout.Label(row.ConfigRole, GUILayout.Width(80));

            EditorGUILayout.BeginHorizontal(GUILayout.Width(240));

            if (row.UtilityReason != null)
            {
                using (new EditorGUI.DisabledScope(true))
                    GUILayout.Label(new GUIContent("Utility Scene", row.UtilityReason), GUILayout.Width(240));
            }
            else
            {
                var setAsMainContent = new GUIContent("Set as Main",
                    "Make this scene the LoadScene (replaces the current main scene). Refused if it has no SceneContext, " +
                    "is already Additive, or (with confirmation) has no GameDatas entry.");
                if (GUILayout.Button(setAsMainContent, GUILayout.Width(90)))
                    SetAsMain(row);

                bool isAdditive = row.ConfigRole == "Additive";
                var additiveContent = new GUIContent(
                    isAdditive ? "Remove Add." : "Add to Add.",
                    isAdditive
                        ? "Remove this scene from AdditiveScenes."
                        : "Add this scene to AdditiveScenes, loaded alongside the Main scene. Refused if it's already Main.");
                if (GUILayout.Button(additiveContent, GUILayout.Width(90)))
                {
                    if (isAdditive)
                        RemoveFromAdditive(row);
                    else
                        AddToAdditive(row);
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);
        }
    }
}
