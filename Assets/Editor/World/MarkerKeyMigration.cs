using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Common.SceneMarkers;
using Infastructure.StaticData;
using Infastructure.StaticData.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Editor.World
{
    /// <summary>
    /// Разовая миграция: добавляет MarkerUniqueId существующим маркерам уровня и переносит
    /// на них ключи из GameData.asset (сопоставление по позиции, ε = 0.01), восстанавливает
    /// осиротевшие записи под "---CONTRACT---", выдаёт свежие ключи маркерам без соответствия.
    /// См. .scratch/plans/jaunty-snuggling-bengio.md, Шаг 2.
    /// Файл удаляется отдельным коммитом после того, как Actor Keys проходит чисто
    /// и Collect идемпотентен — отработавший разовый инструмент.
    /// </summary>
    public static class MarkerKeyMigration
    {
        private const string GameDataAssetPath = "Assets/Resources/StaticData/GameData/GameData.asset";
        private const string ContractRootName = "---CONTRACT---";
        private const float PositionEpsilon = 0.01f;

        private class Report
        {
            public int ComponentsAdded;
            public int KeysTransferred;
            public int MarkersRestored;
            public int FreshKeysIssued;
            public readonly List<(string Type, string Key, Vector3 Position)> Unmatched =
                new List<(string, string, Vector3)>();
        }

        [MenuItem("GD Tools/Migrate/Marker Keys (one-shot)")]
        public static void Run() => Execute(dryRun: false);

        [MenuItem("GD Tools/Migrate/Marker Keys (preview)")]
        public static void Preview() => Execute(dryRun: true);

        private static void Execute(bool dryRun)
        {
            var gameData = AssetDatabase.LoadAssetAtPath<GameStaticData>(GameDataAssetPath);
            if (gameData == null)
            {
                Debug.LogError("Marker Keys migration: GameData.asset не найден.");
                return;
            }

            List<WorldCatalog> catalogs = AssetDatabase.FindAssets("t:WorldCatalog")
                .Select(guid => AssetDatabase.LoadAssetAtPath<WorldCatalog>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(c => c != null)
                .ToList();

            if (catalogs.Count == 0)
            {
                Debug.LogError("Marker Keys migration: в проекте нет ни одного WorldCatalog.");
                return;
            }

            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                if (EditorSceneManager.GetSceneAt(i).isDirty)
                {
                    Debug.LogError("Marker Keys migration: сохрани открытые сцены и запусти снова.");
                    return;
                }
            }

            var report = new Report();

            foreach (WorldCatalog catalog in catalogs)
                MigrateCatalog(catalog, gameData, report, dryRun);

            string prefix = dryRun ? "Marker Keys preview" : "Marker Keys migration";
            Debug.Log($"{prefix}: компонентов добавлено {report.ComponentsAdded}, " +
                $"ключей перенесено {report.KeysTransferred}, маркеров восстановлено {report.MarkersRestored}, " +
                $"выдано новых {report.FreshKeysIssued}.");

            if (!dryRun || report.Unmatched.Count == 0)
                return;

            Debug.Log($"{prefix}: записи без совпадения ({report.Unmatched.Count}):\n" +
                string.Join("\n", report.Unmatched.Select(u => $"  {u.Type} · {u.Key} · {u.Position}")));
        }

        private static void MigrateCatalog(WorldCatalog catalog, GameStaticData gameData, Report report,
            bool dryRun)
        {
            if (string.IsNullOrEmpty(catalog.LevelDataKey))
                return;

            if (!gameData.GameDatas.TryGetValue(catalog.LevelDataKey, out GameData record))
                return;

            List<string> scenePaths = CatalogBuildScenes.BuildDesiredPaths(catalog);
            if (scenePaths.Count == 0)
                return;

            var openedScenes = new List<(string Path, Scene Scene, bool WasOpen)>();

            try
            {
                foreach (string path in scenePaths)
                {
                    Scene scene = EditorSceneManager.GetSceneByPath(path);
                    bool wasOpen = scene.IsValid() && scene.isLoaded;

                    if (!wasOpen)
                        scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

                    openedScenes.Add((path, scene, wasOpen));
                }

                var allMarkers = new List<MarkerBase>();
                foreach ((string _, Scene scene, bool _) in openedScenes)
                    foreach (GameObject root in scene.GetRootGameObjects())
                        allMarkers.AddRange(root.GetComponentsInChildren<MarkerBase>(true));

                foreach (MarkerBase marker in allMarkers)
                    EnsureKeyComponent(marker, report, dryRun);

                // "Целевая сцена" под ---CONTRACT--- — первый (самый нижний) сегмент каталога:
                // сегодня он единственный, и это конечное место маркеров после Шага 3.
                Transform contractRoot = ResolveContractRoot(catalog, openedScenes);
                if (contractRoot == null)
                    Debug.LogWarning($"Marker Keys migration: '{ContractRootName}' не найден для каталога " +
                        $"'{catalog.name}' — восстановленные маркеры лягут в корень первого сегмента.");

                var transferredMarkers = new HashSet<MarkerBase>();

                MigrateList<CheckPointMarker>(record.CheckPoints, allMarkers, contractRoot, report, dryRun,
                    transferredMarkers);
                MigrateList<GeneratorPointMarker>(record.GeneratorPoints, allMarkers, contractRoot, report, dryRun,
                    transferredMarkers);
                MigrateList<BatteryPointMarker>(record.BatteriesPoints, allMarkers, contractRoot, report, dryRun,
                    transferredMarkers);
                MigrateList<EnergyPointMarker>(record.EnergyPoints, allMarkers, contractRoot, report, dryRun,
                    transferredMarkers);
                MigrateList<ElephantPointMarker>(record.ElephantPoints, allMarkers, contractRoot, report, dryRun,
                    transferredMarkers);
                MigrateList<ProductSkillPointMarker>(record.SkillsData, allMarkers, contractRoot, report, dryRun,
                    transferredMarkers);

                foreach (MarkerBase marker in allMarkers)
                    AssignFreshKeyIfNeeded(marker, report, dryRun, transferredMarkers);

                if (!dryRun)
                    foreach ((string _, Scene scene, bool _) in openedScenes)
                        if (scene.isDirty)
                            EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                foreach ((string _, Scene scene, bool wasOpen) in openedScenes)
                    if (!wasOpen && scene.IsValid())
                        EditorSceneManager.CloseScene(scene, removeScene: true);
            }
        }

        private static void EnsureKeyComponent(MarkerBase marker, Report report, bool dryRun)
        {
            if (marker.GetComponent<MarkerUniqueId>() != null)
                return;

            report.ComponentsAdded++;
            if (dryRun)
                return;

            Undo.AddComponent<MarkerUniqueId>(marker.gameObject);
            EditorUtility.SetDirty(marker.gameObject);
            EditorSceneManager.MarkSceneDirty(marker.gameObject.scene);
        }

        private static Transform ResolveContractRoot(WorldCatalog catalog,
            List<(string Path, Scene Scene, bool WasOpen)> openedScenes)
        {
            SegmentDefinition firstSegment = catalog.Segments.FirstOrDefault();
            if (firstSegment == null || firstSegment.Scene == null || firstSegment.Scene.SceneAsset == null)
                return null;

            string segmentPath = AssetDatabase.GetAssetPath(firstSegment.Scene.SceneAsset);
            Scene segmentScene = openedScenes.FirstOrDefault(s => s.Path == segmentPath).Scene;
            if (!segmentScene.IsValid())
                return null;

            foreach (GameObject root in segmentScene.GetRootGameObjects())
                if (root.name == ContractRootName)
                    return root.transform;

            return null;
        }

        private static void MigrateList<T>(IEnumerable<WorldData> records, List<MarkerBase> allMarkers,
            Transform contractRoot, Report report, bool dryRun, HashSet<MarkerBase> transferredMarkers)
            where T : MarkerBase
        {
            if (records == null)
                return;

            List<T> unmatched = allMarkers.OfType<T>().ToList();
            float epsilonSqr = PositionEpsilon * PositionEpsilon;

            foreach (WorldData recordEntry in records)
            {
                if (string.IsNullOrEmpty(recordEntry.UniqueId))
                    continue;

                T nearest = null;
                float nearestDistSqr = epsilonSqr;
                int candidateCount = 0;

                foreach (T candidate in unmatched)
                {
                    float distSqr = (candidate.transform.position - recordEntry.WorldPosition).sqrMagnitude;
                    if (distSqr <= epsilonSqr)
                        candidateCount++;

                    if (distSqr <= nearestDistSqr)
                    {
                        nearest = candidate;
                        nearestDistSqr = distSqr;
                    }
                }

                if (candidateCount > 1)
                    Debug.LogWarning($"Marker Keys migration: {candidateCount} кандидатов в ε для записи " +
                        $"'{recordEntry.UniqueId}' у {recordEntry.WorldPosition} — разобрать вручную.");

                if (nearest != null)
                {
                    unmatched.Remove(nearest);
                    transferredMarkers.Add(nearest);
                    TransferKey(nearest, recordEntry.UniqueId, report, dryRun);
                }
                else
                {
                    RestoreMarker<T>(recordEntry, contractRoot, report, dryRun);
                }
            }
        }

        private static void TransferKey(MarkerBase marker, string key, Report report, bool dryRun)
        {
            report.KeysTransferred++;
            if (dryRun)
                return;

            MarkerUniqueId uniqueId = marker.GetComponent<MarkerUniqueId>();
            Undo.RecordObject(uniqueId, "Migrate marker key");
            uniqueId.UniqueId = key;
            PrefabUtility.RecordPrefabInstancePropertyModifications(uniqueId);
            EditorUtility.SetDirty(uniqueId);
            EditorSceneManager.MarkSceneDirty(uniqueId.gameObject.scene);
        }

        private static void RestoreMarker<T>(WorldData recordEntry, Transform contractRoot, Report report,
            bool dryRun) where T : MarkerBase
        {
            report.Unmatched.Add((typeof(T).Name, recordEntry.UniqueId, recordEntry.WorldPosition));
            report.MarkersRestored++;
            if (dryRun)
                return;

            var go = new GameObject(typeof(T).Name);
            Undo.RegisterCreatedObjectUndo(go, "Restore marker from GameData");

            if (contractRoot != null)
                go.transform.SetParent(contractRoot, worldPositionStays: false);

            go.transform.position = recordEntry.WorldPosition;
            go.transform.rotation = recordEntry.WorldRotation;

            T marker = go.AddComponent<T>();
            if (marker is ProductSkillPointMarker skillMarker && recordEntry is ProductSkillData skillData)
                skillMarker.ProductType = skillData.ProductType;

            MarkerUniqueId uniqueId = go.GetComponent<MarkerUniqueId>();
            uniqueId.UniqueId = recordEntry.UniqueId;

            EditorUtility.SetDirty(go);
            EditorSceneManager.MarkSceneDirty(go.scene);
        }

        private static void AssignFreshKeyIfNeeded(MarkerBase marker, Report report, bool dryRun,
            HashSet<MarkerBase> transferredMarkers)
        {
            if (transferredMarkers.Contains(marker))
                return;

            MarkerUniqueId uniqueId = marker.GetComponent<MarkerUniqueId>();

            if (dryRun)
            {
                if (uniqueId != null && !string.IsNullOrEmpty(uniqueId.UniqueId))
                    return;

                report.FreshKeysIssued++;
                return;
            }

            if (uniqueId == null || !string.IsNullOrEmpty(uniqueId.UniqueId))
                return;

            Undo.RecordObject(uniqueId, "Assign fresh marker key");
            uniqueId.UniqueId = Guid.NewGuid().ToString();
            PrefabUtility.RecordPrefabInstancePropertyModifications(uniqueId);
            EditorUtility.SetDirty(uniqueId);
            EditorSceneManager.MarkSceneDirty(uniqueId.gameObject.scene);
            report.FreshKeysIssued++;
        }
    }
}
