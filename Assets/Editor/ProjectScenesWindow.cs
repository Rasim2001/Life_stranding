using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Infastructure.StaticData;
using Infastructure.StaticData.World;
using Editor.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Editor
{
    /// <summary>
    /// Overview of project scenes: where logic lives (SceneContext) vs pure content,
    /// how many markers, whether there's a GameDatas entry, Build Settings and config state.
    /// Switching the entry/segment scenes and launching Play from any scene.
    /// Scene content is determined by text-parsing .unity (YAML), without opening scenes.
    /// Configuration source is WorldCatalog (Assets/Scripts/Infastructure/StaticData/World) —
    /// see .scratch/additive-scenes-vertical/spec.md. Tool behavior is meant to survive
    /// config changes unchanged.
    /// </summary>
    public class ProjectScenesWindow : EditorWindow
    {
        private const string ScenesRoot = "Assets/Scenes/";
        private const string BootstrapScenePath = "Assets/Scenes/Bootstrap.unity";
        private const string GameDataAssetPath = "Assets/Resources/StaticData/GameData/GameData.asset";
        private const string SceneContextScriptPath = "Assets/Plugins/Zenject/Source/Install/Contexts/SceneContext.cs";
        private const string SegmentInjectorScriptPath = "Assets/Scripts/Infastructure/World/SegmentInjector.cs";
        private const string SegmentsFolderPath = "Assets/Settings/World/Segments";

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
        // entry or a segment scene doesn't crash, but silently breaks the game (empty
        // level with no SceneInstaller bindings, or a broken defeat/pause/win exit flow).
        private static readonly Dictionary<string, string> UtilitySceneReasons = new Dictionary<string, string>
        {
            ["Bootstrap"] =
                "Entry-point scene: bare SceneContext, no SceneInstaller. " +
                "Setting it as Entry/Segment produces an empty level — world/UI bindings never run.",
            ["ExitGameLoop"] =
                "Transitional unload scene used internally by ExitGameLoopState " +
                "(defeat/pause/win flow) — not a content scene.",
        };

        private readonly List<SceneRow> _rows = new List<SceneRow>();
        private readonly Dictionary<string, string> _scenePathByName = new Dictionary<string, string>();
        private GameStaticData _gameData;
        private WorldCatalog _catalog;
        private CatalogBuildScenes.Diff _buildDiff;
        private Vector2 _scroll;
        private string _lastRefreshInfo = "";

        private class SceneRow
        {
            public string Name;
            public string Path;
            public bool HasSceneContext;
            public bool HasSegmentInjector;
            public int[] MarkerCounts;
            public bool HasGameDataEntry;
            public int GameDataPointsCount;
            public string BuildSettingsState;
            public string BuildSettingsPlanned;
            public int ApproxObjectCount;
            public string ConfigRole;
            public string UtilityReason;
            public SegmentDefinition OwnerSegment;
        }

        [MenuItem("GD Tools/Scenes")]
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
            string segmentInjectorGuid = AssetDatabase.AssetPathToGUID(SegmentInjectorScriptPath);
            string[] markerGuids = MarkerTypes.Select(m => AssetDatabase.AssetPathToGUID(m.ScriptPath)).ToArray();

            _gameData = AssetDatabase.LoadAssetAtPath<GameStaticData>(GameDataAssetPath);
            GameStaticData gameData = _gameData;
            _catalog = gameData != null ? gameData.WorldCatalog : null;
            _buildDiff = CatalogBuildScenes.Compare(_catalog);

            // path -> (role, owning segment; null for Entry)
            var roleByPath = new Dictionary<string, (string Role, SegmentDefinition Owner)>();
            if (_catalog != null)
            {
                string atmospherePath = CatalogBuildScenes.GetScenePath(_catalog.AtmosphereScene);
                if (!string.IsNullOrEmpty(atmospherePath))
                    roleByPath[atmospherePath] = ("Atmosphere", null);

                string entryPath = CatalogBuildScenes.GetScenePath(_catalog.EntryScene);
                if (!string.IsNullOrEmpty(entryPath))
                    roleByPath[entryPath] = ("Entry", null);

                foreach (SegmentDefinition segment in _catalog.Segments)
                {
                    if (segment == null)
                        continue;

                    string scenePath = CatalogBuildScenes.GetScenePath(segment.Scene);
                    if (!string.IsNullOrEmpty(scenePath) && !roleByPath.ContainsKey(scenePath))
                        roleByPath[scenePath] = ("Segment", segment);

                    foreach (SceneReference additional in segment.AdditionalScenes)
                    {
                        string additionalPath = CatalogBuildScenes.GetScenePath(additional);
                        if (!string.IsNullOrEmpty(additionalPath) && !roleByPath.ContainsKey(additionalPath))
                            roleByPath[additionalPath] = ("Segment+", segment);
                    }
                }
            }

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
                    HasSegmentInjector = !string.IsNullOrEmpty(segmentInjectorGuid) && text.Contains(segmentInjectorGuid),
                    MarkerCounts = markerGuids
                        .Select(guid => string.IsNullOrEmpty(guid) ? 0 : CountOccurrences(text, guid))
                        .ToArray(),
                    ApproxObjectCount = CountOccurrences(text, "\n--- !u!1 &"),
                    BuildSettingsState = GetBuildSettingsState(path),
                    BuildSettingsPlanned = CatalogBuildScenes.DescribeRowState(path, _buildDiff),
                    ConfigRole = "—",
                };

                UtilitySceneReasons.TryGetValue(row.Name, out row.UtilityReason);

                if (gameData != null && gameData.GameDatas != null &&
                    gameData.GameDatas.TryGetValue(row.Name, out GameData gd))
                {
                    row.HasGameDataEntry = true;
                    row.GameDataPointsCount = CountGameDataPoints(gd);
                }

                if (roleByPath.TryGetValue(path, out (string Role, SegmentDefinition Owner) info))
                {
                    row.ConfigRole = info.Role;
                    row.OwnerSegment = info.Owner;
                }

                _scenePathByName[row.Name] = path;
                _rows.Add(row);
            }

            // Group rows top to bottom: Utility scenes first (they're noise, not levels),
            // then Logic scenes (candidate Entry), then Content scenes (candidate Segment).
            // Within a group: current Entry first, then Segment, then Build Settings-enabled,
            // then alphabetical. Renaming scenes into real name-based groups is a separate
            // task (see docs/asset-organization-and-naming.md §6.6) — this is just a stopgap.
            List<SceneRow> sorted = _rows
                .OrderBy(r => GetGroupIndex(r))
                .ThenByDescending(r => r.ConfigRole == "Entry")
                .ThenByDescending(r => r.ConfigRole == "Segment" || r.ConfigRole == "Segment+")
                .ThenByDescending(r => r.BuildSettingsState == "enabled")
                .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            _rows.Clear();
            _rows.AddRange(sorted);

            _lastRefreshInfo = $"Scenes: {_rows.Count} · refreshed {DateTime.Now:HH:mm:ss}";
        }

        private void SetAsMain(SceneRow row)
        {
            if (_catalog == null)
            {
                EditorUtility.DisplayDialog("No WorldCatalog", "GameData.asset has no WorldCatalog assigned.", "Got it");
                return;
            }

            if (row.UtilityReason != null)
            {
                EditorUtility.DisplayDialog("Utility scene",
                    $"«{row.Name}» is a utility scene, not a level: {row.UtilityReason}",
                    "Got it");
                return;
            }

            if (!row.HasSceneContext)
            {
                EditorUtility.DisplayDialog("Can't set as Entry",
                    $"Scene «{row.Name}» has no SceneContext — SceneInstaller and BuildLevelState won't run, the spider won't appear.",
                    "Got it");
                return;
            }

            if (row.ConfigRole == "Segment" || row.ConfigRole == "Segment+")
            {
                EditorUtility.DisplayDialog("Can't set as Entry",
                    $"Scene «{row.Name}» is already part of a segment — Unity would load a second copy of the geometry. Remove it from the segment first.",
                    "Got it");
                return;
            }

            if (row.ConfigRole == "Atmosphere")
            {
                EditorUtility.DisplayDialog("Can't set as Entry",
                    $"Scene «{row.Name}» is already Atmosphere — Unity would load it twice, Single and additively.",
                    "Got it");
                return;
            }

            bool hasLevelDataEntry = !string.IsNullOrEmpty(_catalog.LevelDataKey) &&
                _gameData.GameDatas != null && _gameData.GameDatas.ContainsKey(_catalog.LevelDataKey);

            if (!hasLevelDataEntry)
            {
                bool proceed = EditorUtility.DisplayDialog("No GameDatas entry",
                    $"WorldCatalog.LevelDataKey «{_catalog.LevelDataKey}» has no entry in GameDatas — the runtime will throw KeyNotFoundException in GameFactory. Set as Entry anyway?",
                    "Set anyway", "Cancel");
                if (!proceed)
                    return;
            }

            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(row.Path);

            Undo.RecordObject(_catalog, "Set EntryScene");
            _catalog.SetEntryScene(sceneAsset);
            EditorUtility.SetDirty(_catalog);
            AssetDatabase.SaveAssets();

            Refresh();
        }

        private void SetAsAtmosphere(SceneRow row)
        {
            if (_catalog == null)
            {
                EditorUtility.DisplayDialog("No WorldCatalog", "GameData.asset has no WorldCatalog assigned.", "Got it");
                return;
            }

            if (row.UtilityReason != null)
            {
                EditorUtility.DisplayDialog("Utility scene",
                    $"«{row.Name}» is a utility scene, not a level: {row.UtilityReason}",
                    "Got it");
                return;
            }

            if (row.ConfigRole == "Entry" || row.ConfigRole == "Segment" || row.ConfigRole == "Segment+")
            {
                EditorUtility.DisplayDialog("Can't set as Atmosphere",
                    $"Scene «{row.Name}» is already {row.ConfigRole} — the same scene can't also be the atmosphere scene.",
                    "Got it");
                return;
            }

            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(row.Path);

            Undo.RecordObject(_catalog, "Set AtmosphereScene");
            _catalog.SetAtmosphereScene(sceneAsset);
            EditorUtility.SetDirty(_catalog);
            AssetDatabase.SaveAssets();

            Refresh();
        }

        private void SetShowsFirstEncounter(bool value)
        {
            if (_catalog == null)
                return;

            Undo.RecordObject(_catalog, "Set ShowsFirstEncounter");
            _catalog.SetShowsFirstEncounter(value);
            EditorUtility.SetDirty(_catalog);
            AssetDatabase.SaveAssets();
        }

        private void AddToAdditive(SceneRow row)
        {
            if (_catalog == null)
            {
                EditorUtility.DisplayDialog("No WorldCatalog", "GameData.asset has no WorldCatalog assigned.", "Got it");
                return;
            }

            if (row.UtilityReason != null)
            {
                EditorUtility.DisplayDialog("Utility scene",
                    $"«{row.Name}» is a utility scene, not a level: {row.UtilityReason}",
                    "Got it");
                return;
            }

            if (row.ConfigRole == "Entry")
            {
                EditorUtility.DisplayDialog("Can't add as segment",
                    $"Scene «{row.Name}» is already Entry — Unity would load a second copy of the geometry.",
                    "Got it");
                return;
            }

            if (row.ConfigRole == "Atmosphere")
            {
                EditorUtility.DisplayDialog("Can't add as segment",
                    $"Scene «{row.Name}» is already Atmosphere — Unity would load a second copy of the geometry.",
                    "Got it");
                return;
            }

            if (row.OwnerSegment != null)
                return;

            if (!AssetDatabase.IsValidFolder(SegmentsFolderPath))
                AssetDatabase.CreateFolder("Assets/Settings/World", "Segments");

            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(row.Path);

            var segment = ScriptableObject.CreateInstance<SegmentDefinition>();
            segment.SetScene(sceneAsset);

            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{SegmentsFolderPath}/DATA_Segment_{row.Name}.asset");
            AssetDatabase.CreateAsset(segment, assetPath);

            Undo.RecordObject(_catalog, "Add Segment to Catalog");
            _catalog.AddSegment(segment);
            EditorUtility.SetDirty(_catalog);
            AssetDatabase.SaveAssets();

            Refresh();
        }

        private void RemoveFromAdditive(SceneRow row)
        {
            if (_catalog == null || row.OwnerSegment == null || row.ConfigRole != "Segment")
                return;

            Undo.RecordObject(_catalog, "Remove Segment from Catalog");
            _catalog.RemoveSegment(row.OwnerSegment);
            EditorUtility.SetDirty(_catalog);
            AssetDatabase.SaveAssets();

            Refresh();
        }

        private void OpenConfiguredSet()
        {
            if (_catalog == null)
                return;

            string atmospherePath = CatalogBuildScenes.GetScenePath(_catalog.AtmosphereScene);
            if (string.IsNullOrEmpty(atmospherePath) || !_scenePathByName.ContainsValue(atmospherePath))
            {
                EditorUtility.DisplayDialog("Not found",
                    "Atmosphere scene file was not found among scanned scenes.",
                    "OK");
                return;
            }

            string entryPath = CatalogBuildScenes.GetScenePath(_catalog.EntryScene);
            if (string.IsNullOrEmpty(entryPath) || !_scenePathByName.ContainsValue(entryPath))
            {
                EditorUtility.DisplayDialog("Not found",
                    "Entry scene file was not found among scanned scenes.",
                    "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EditorSceneManager.OpenScene(atmospherePath, OpenSceneMode.Single);
            EditorSceneManager.OpenScene(entryPath, OpenSceneMode.Additive);

            foreach (SegmentDefinition segment in _catalog.Segments)
            {
                if (segment == null)
                    continue;

                foreach (SceneReference sceneReference in segment.SceneReferences)
                {
                    string path = CatalogBuildScenes.GetScenePath(sceneReference);
                    if (!string.IsNullOrEmpty(path))
                        EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                }
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

            if (_catalog == null || !string.Equals(_catalog.EntryScene?.SceneName, sceneName, StringComparison.Ordinal))
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

        // Read-only sanity check over the currently SAVED config (not the open scene, not
        // unsaved edits). Doesn't write anything. Catches drift from manual .asset editing
        // that bypasses this tool's guards, and blind spots the tool doesn't otherwise
        // surface.
        private void ValidateConfiguration()
        {
            if (_gameData == null)
            {
                EditorUtility.DisplayDialog("Validate Configuration", "GameData.asset not found.", "OK");
                return;
            }

            if (_catalog == null)
            {
                EditorUtility.DisplayDialog("Validate Configuration", "GameData.asset has no WorldCatalog assigned.", "OK");
                return;
            }

            string entryPath = CatalogBuildScenes.GetScenePath(_catalog.EntryScene);
            SceneRow entryRow = _rows.FirstOrDefault(r => r.Path == entryPath);

            string atmospherePath = CatalogBuildScenes.GetScenePath(_catalog.AtmosphereScene);
            SceneRow atmosphereRow = _rows.FirstOrDefault(r => r.Path == atmospherePath);

            List<SegmentDefinition> segments = _catalog.Segments.ToList();
            List<SceneReference> allSceneReferences = segments
                .Where(s => s != null)
                .SelectMany(s => s.SceneReferences)
                .ToList();

            List<SegmentBakedData> orderedBands = SegmentBands.ReadBands(_catalog).Values
                .Where(b => b != null && b.IsValid)
                .OrderBy(b => b.BottomY)
                .ToList();

            bool hasNonAdjacentOverlap = false;
            bool hasGap = false;
            for (int i = 0; i < orderedBands.Count; i++)
            {
                for (int j = i + 1; j < orderedBands.Count; j++)
                {
                    bool overlaps = orderedBands[i].TopY > orderedBands[j].BottomY;
                    if (overlaps && j != i + 1)
                        hasNonAdjacentOverlap = true;
                }

                if (i + 1 < orderedBands.Count && orderedBands[i + 1].BottomY > orderedBands[i].TopY)
                    hasGap = true;
            }

            var checks = new List<(string Label, bool Passed, bool IsWarning)>
            {
                ("EntryScene is set and exists among scanned scenes", entryRow != null, false),
                ("EntryScene has SceneContext", entryRow != null && entryRow.HasSceneContext, false),
                ("EntryScene is enabled in Build Settings", entryRow != null && entryRow.BuildSettingsState == "enabled", false),
                ("LevelDataKey is set and exists in GameDatas",
                    !string.IsNullOrEmpty(_catalog.LevelDataKey) &&
                    _gameData.GameDatas != null && _gameData.GameDatas.ContainsKey(_catalog.LevelDataKey), false),
                ("EntryScene does not overlap segment scenes",
                    string.IsNullOrEmpty(entryPath) || allSceneReferences.All(r => CatalogBuildScenes.GetScenePath(r) != entryPath), false),
                ("Every segment scene reference is valid (SceneAsset assigned)",
                    allSceneReferences.All(r => r != null && r.IsValid), false),
                ("No scene is referenced by two different segments",
                    allSceneReferences.Where(r => r != null && r.IsValid).GroupBy(r => r.SceneName).All(g => g.Count() == 1), false),
                ("No SegmentDefinition is listed twice in the catalog", segments.Distinct().Count() == segments.Count, false),
                ("No utility scene (Bootstrap/ExitGameLoop) is set as Entry, Atmosphere or Segment",
                    (entryRow == null || !UtilitySceneReasons.ContainsKey(entryRow.Name)) &&
                    (atmosphereRow == null || !UtilitySceneReasons.ContainsKey(atmosphereRow.Name)) &&
                    _rows.Where(r => r.ConfigRole == "Segment" || r.ConfigRole == "Segment+")
                        .All(r => !UtilitySceneReasons.ContainsKey(r.Name)), false),
                ("Build Settings matches the catalog", _buildDiff.InSync, false),
                ("Bootstrap scene is first in Build Settings",
                    EditorBuildSettings.scenes.Length > 0 &&
                    EditorBuildSettings.scenes[0].path == CatalogBuildScenes.GetScenePath(_catalog.BootstrapScene) &&
                    EditorBuildSettings.scenes[0].enabled, false),
                ("AtmosphereScene is set and exists among scanned scenes", atmosphereRow != null, false),
                ("AtmosphereScene has no SceneContext", atmosphereRow != null && !atmosphereRow.HasSceneContext, false),
                ("AtmosphereScene does not overlap EntryScene or segment scenes",
                    string.IsNullOrEmpty(atmospherePath) ||
                    (atmospherePath != entryPath && allSceneReferences.All(r => CatalogBuildScenes.GetScenePath(r) != atmospherePath)), false),
                ("AtmosphereScene is enabled in Build Settings", atmosphereRow != null && atmosphereRow.BuildSettingsState == "enabled", false),
                ("Every segment scene has a SegmentInjector",
                    _rows.Where(r => r.ConfigRole == "Segment" || r.ConfigRole == "Segment+")
                        .All(r => r.HasSegmentInjector), false),
                ("Every catalog segment has a valid SegmentBakedData band",
                    segments.Where(s => s != null).All(s => s.Baked != null && s.Baked.IsValid), false),
                ("No band overlap between non-adjacent segments", !hasNonAdjacentOverlap, false),
                ("No height gap between neighboring segment bands", !hasGap, true),
                ("No segment band top is above 10000 (scene-architecture.md §7.4)",
                    orderedBands.All(b => b.TopY <= 10000f), true),
            };

            foreach ((string label, bool passed, bool isWarning) in checks)
            {
                if (passed)
                    continue;

                if (isWarning)
                    Debug.LogWarning($"[ProjectScenesWindow] Validation warning: {label}");
                else
                    Debug.LogError($"[ProjectScenesWindow] Validation failed: {label}");
            }

            string report = string.Join("\n", checks.Select(c =>
                (c.Passed ? "✓ " : c.IsWarning ? "⚠ " : "✗ ") + c.Label));
            EditorUtility.DisplayDialog("Validate Configuration", report, "OK");
        }

        private void SyncSegmentBands()
        {
            if (_catalog == null)
            {
                EditorUtility.DisplayDialog("Sync Segment Bands", "GameData.asset has no WorldCatalog assigned.", "OK");
                return;
            }

            List<SegmentBands.SyncResult> results = SegmentBands.Sync(_catalog);

            string report = results.Count == 0
                ? "No segments in the catalog."
                : string.Join("\n", results.Select(r =>
                    $"{(r.Success ? "✓" : "✗")} {r.SceneName ?? "?"}: {r.Message}"));

            EditorUtility.DisplayDialog("Sync Segment Bands", report, "OK");
            Refresh();
        }

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
            if (_catalog == null || _catalog.EntryScene == null || !_catalog.EntryScene.IsValid)
                return "(EntryScene not set)";

            IReadOnlyList<SegmentDefinition> catalogSegments = _catalog.Segments;

            string segments = catalogSegments.Count > 0
                ? " + " + string.Join(" + ", catalogSegments.Select(s => s.Scene?.SceneName ?? "?"))
                : "";

            string atmosphere = _catalog.AtmosphereScene != null && _catalog.AtmosphereScene.IsValid
                ? _catalog.AtmosphereScene.SceneName
                : "(no atmosphere)";

            return $"{atmosphere} → {_catalog.EntryScene.SceneName}{segments}";
        }

        private string GetBuildDiffSummary()
        {
            if (_buildDiff.Blockers.Count > 0)
                return $"blocked: {_buildDiff.Blockers[0]}";

            if (_buildDiff.InSync)
                return "in sync";

            var parts = new List<string>();
            if (_buildDiff.ToAdd.Count > 0)
                parts.Add($"{_buildDiff.ToAdd.Count} add");
            if (_buildDiff.ToEnable.Count > 0)
                parts.Add($"{_buildDiff.ToEnable.Count} enable");
            if (_buildDiff.ToDisable.Count > 0)
                parts.Add($"{_buildDiff.ToDisable.Count} disable");
            if (_buildDiff.ReorderNeeded)
                parts.Add("reorder");

            return "+" + string.Join(" · ", parts);
        }

        private void ApplyCatalogToBuildSettings()
        {
            if (_buildDiff.Blockers.Count > 0)
            {
                EditorUtility.DisplayDialog("Apply Catalog",
                    "Can't apply — " + string.Join("; ", _buildDiff.Blockers), "OK");
                return;
            }

            if (_buildDiff.InSync)
            {
                EditorUtility.DisplayDialog("Apply Catalog", "Build Settings already matches the catalog.", "OK");
                return;
            }

            string Describe(string label, List<string> paths) =>
                paths.Count == 0 ? null : $"{label}:\n" + string.Join("\n", paths.Select(p => "  " + p));

            string message = string.Join("\n\n", new[]
                {
                    Describe("Add", _buildDiff.ToAdd),
                    Describe("Enable", _buildDiff.ToEnable),
                    Describe("Disable", _buildDiff.ToDisable),
                    _buildDiff.ReorderNeeded ? "Reorder enabled scenes to match the catalog order." : null,
                }.Where(s => s != null));

            bool proceed = EditorUtility.DisplayDialog("Apply Catalog",
                message + "\n\nEditorBuildSettings has no Undo — review before confirming.",
                "Apply", "Cancel");
            if (!proceed)
                return;

            CatalogBuildScenes.Apply(_catalog);
            Refresh();
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
                ? "Logic scenes (candidate Entry)"
                : "Content scenes (candidate Segment)";
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
                    "Enter Play mode via Bootstrap, which then loads the scene set shown below (EntryScene + segment scenes).",
                    140, Play);

                string activeSceneName = EditorSceneManager.GetActiveScene().name;
                DrawActionButton("Play This Scene", activeSceneName,
                    "Set the currently open scene as EntryScene (same validation as 'Set as Entry'), then enter Play mode via Bootstrap.",
                    140, PlayThisScene);

                DrawActionButton("Open Set", GetConfiguredSetSummary(),
                    "Open the currently configured Entry scene (Single) plus all segment scenes in the editor, without entering Play mode.",
                    140, OpenConfiguredSet);
            }

            SceneAsset startScene = EditorSceneManager.playModeStartScene;
            DrawActionButton("Reset Start Scene", startScene != null ? startScene.name : "default (not set)",
                "Clear playModeStartScene so normal Unity Play behavior (start from the currently open scene) is restored.",
                140, ResetPlayModeStartScene);

            DrawActionButton("Sync Segment Bands", "bake SegmentBounds",
                "Open each segment scene, read its SegmentBounds component, and write/update the matching " +
                "SegmentBakedData asset (Assets/Settings/World/Segments/Generated). Requires all scenes saved.",
                140, SyncSegmentBands);

            DrawActionButton("Validate Configuration", "read-only check",
                "Run a checklist against the currently saved GameData.asset / WorldCatalog (Entry/Segment consistency, " +
                "Build Settings, GameDatas entries, utility-scene misuse). Doesn't write anything.",
                140, ValidateConfiguration);

            DrawActionButton("Apply Catalog", GetBuildDiffSummary(),
                "Rewrite EditorBuildSettings.scenes to match the catalog: bootstrap first, then resident scenes, " +
                "Entry, then segment scenes; anything else is disabled, not removed. No Undo for this operation — " +
                "confirm the preview dialog before applying.",
                140, ApplyCatalogToBuildSettings);

            if (_catalog != null)
            {
                bool current = _catalog.ShowsFirstEncounter;
                bool updated = EditorGUILayout.ToggleLeft("First-encounter popups", current, GUILayout.Width(160));
                if (updated != current)
                    SetShowsFirstEncounter(updated);
            }

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
            GUILayout.Label("Config Role", EditorStyles.boldLabel, GUILayout.Width(120));
            GUILayout.Label("Actions", EditorStyles.boldLabel, GUILayout.Width(390));
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

            GUILayout.Label(row.BuildSettingsPlanned, GUILayout.Width(90));
            GUILayout.Label(row.ApproxObjectCount.ToString(), GUILayout.Width(70));
            GUILayout.Label(row.ConfigRole, GUILayout.Width(120));

            EditorGUILayout.BeginHorizontal(GUILayout.Width(390));

            if (row.UtilityReason != null)
            {
                using (new EditorGUI.DisabledScope(true))
                    GUILayout.Label(new GUIContent("Utility Scene", row.UtilityReason), GUILayout.Width(390));
            }
            else
            {
                var setAsMainContent = new GUIContent("Set as Entry",
                    "Make this scene the EntryScene (replaces the current entry scene). Refused if it has no SceneContext, " +
                    "is already part of a segment, or (with confirmation) has no GameDatas entry.");
                if (GUILayout.Button(setAsMainContent, GUILayout.Width(90)))
                    SetAsMain(row);

                var setAsAtmosphereContent = new GUIContent("Set as Atm.",
                    "Make this scene the AtmosphereScene (replaces the current atmosphere scene). Refused if it's " +
                    "already Entry or part of a segment.");
                if (GUILayout.Button(setAsAtmosphereContent, GUILayout.Width(90)))
                    SetAsAtmosphere(row);

                bool isSegment = row.OwnerSegment != null;
                bool isPrimarySegmentScene = row.ConfigRole == "Segment";
                var additiveContent = new GUIContent(
                    isSegment ? "Remove Add." : "Add to Add.",
                    isSegment
                        ? (isPrimarySegmentScene
                            ? "Remove this scene's segment from the catalog. The SegmentDefinition asset itself is kept, not deleted."
                            : "This scene is an AdditionalScenes entry of a segment — removing single additional scenes isn't supported by this tool. Edit the SegmentDefinition asset directly.")
                        : "Create a SegmentDefinition for this scene and add it to the catalog. Refused if it's already Entry.");

                using (new EditorGUI.DisabledScope(isSegment && !isPrimarySegmentScene))
                {
                    if (GUILayout.Button(additiveContent, GUILayout.Width(90)))
                    {
                        if (isSegment)
                            RemoveFromAdditive(row);
                        else
                            AddToAdditive(row);
                    }
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);
        }
    }
}
