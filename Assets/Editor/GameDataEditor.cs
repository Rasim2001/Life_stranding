using System.Collections.Generic;
using System.Linq;
using Common.SceneMarkers;
using Editor.World;
using Infastructure.StaticData;
using Infastructure.StaticData.World;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Editor
{
    [CustomEditor(typeof(GameStaticData))]
    public class GameDataEditor : OdinEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var gameData = (GameStaticData)target;

            if (GUILayout.Button("Collect"))
                Collect(gameData);
        }

        private static void Collect(GameStaticData gameData)
        {
            WorldCatalog catalog = gameData.WorldCatalog;
            if (catalog == null || string.IsNullOrEmpty(catalog.LevelDataKey))
            {
                Debug.LogError("Collect: WorldCatalog не назначен или LevelDataKey пуст.");
                return;
            }

            List<string> catalogScenePaths = CatalogContentScenes.CollectScenePaths(catalog);
            if (catalogScenePaths.Count == 0)
            {
                Debug.LogError("Collect: в WorldCatalog не настроено ни одной сцены.");
                return;
            }

            var catalogScenes = new List<Scene>();
            var unloadedPaths = new List<string>();

            foreach (string path in catalogScenePaths)
            {
                Scene scene = EditorSceneManager.GetSceneByPath(path);
                if (!scene.IsValid() || !scene.isLoaded)
                    unloadedPaths.Add(path);
                else
                    catalogScenes.Add(scene);
            }

            if (unloadedPaths.Count > 0)
            {
                Debug.LogError("Collect: не загружены сцены каталога:\n" +
                    string.Join("\n", unloadedPaths.Select(p => "  " + p)));
                return;
            }

            var catalogScenePathSet = new HashSet<string>(catalogScenePaths);

            var collected = new List<MarkerBase>();
            foreach (Scene scene in catalogScenes)
                foreach (GameObject root in scene.GetRootGameObjects())
                    collected.AddRange(root.GetComponentsInChildren<MarkerBase>(true));

            var skipped = new List<MarkerBase>();
            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                Scene scene = EditorSceneManager.GetSceneAt(i);
                if (!scene.isLoaded || catalogScenePathSet.Contains(scene.path))
                    continue;

                foreach (GameObject root in scene.GetRootGameObjects())
                    skipped.AddRange(root.GetComponentsInChildren<MarkerBase>(true));
            }

            if (skipped.Count > 0)
                Debug.LogWarning("Collect: маркеры вне каталога пропущены (не собраны):\n" +
                    string.Join("\n", skipped.Select(m =>
                        $"  {m.gameObject.scene.path} · {CatalogContentScenes.GetHierarchyPath(m.transform)}")));

            var spiderSpawns = collected.OfType<SpiderSpawnPointMarker>().ToList();
            if (spiderSpawns.Count != 1)
            {
                Debug.LogError($"Collect: найдено SpiderSpawnPointMarker — {spiderSpawns.Count}, требуется ровно 1.");
                return;
            }

            var flowers = collected.OfType<FlowerPointMarker>().ToList();
            if (flowers.Count != 1)
            {
                Debug.LogError($"Collect: найдено FlowerPointMarker — {flowers.Count}, требуется ровно 1.");
                return;
            }

            List<MarkerBase> withoutKey = collected.Where(m => string.IsNullOrEmpty(m.UniqueId)).ToList();
            if (withoutKey.Count > 0)
            {
                Debug.LogError("Collect: маркеры без ключа памяти (нет MarkerUniqueId или ключ пуст):\n" +
                    string.Join("\n", withoutKey.Select(m => "  " + CatalogContentScenes.GetHierarchyPath(m.transform))));
                return;
            }

            List<IGrouping<string, MarkerBase>> duplicateGroups =
                collected.GroupBy(m => m.UniqueId).Where(g => g.Count() > 1).ToList();
            if (duplicateGroups.Count > 0)
            {
                Debug.LogError("Collect: дубликаты ключей:\n" + string.Join("\n", duplicateGroups.Select(g =>
                    $"  {g.Key}:\n" + string.Join("\n", g.Select(m => "    " + CatalogContentScenes.GetHierarchyPath(m.transform))))));
                return;
            }

            var data = new GameData
            {
                CheckPoints = collected.OfType<CheckPointMarker>()
                    .OrderBy(m => m.transform.GetSiblingIndex())
                    .Select(ToWorldData)
                    .ToList(),
                GeneratorPoints = collected.OfType<GeneratorPointMarker>()
                    .OrderBy(m => m.transform.GetSiblingIndex())
                    .Select(ToWorldData)
                    .ToList(),
                BatteriesPoints = collected.OfType<BatteryPointMarker>().Select(ToWorldData).ToList(),
                EnergyPoints = collected.OfType<EnergyPointMarker>().Select(ToWorldData).ToList(),
                ElephantPoints = collected.OfType<ElephantPointMarker>().Select(ToWorldData).ToList(),
                SkillsData = collected.OfType<ProductSkillPointMarker>()
                    .Select(m => new ProductSkillData(m.transform.position, m.transform.rotation, m.ProductType,
                        m.UniqueId))
                    .ToList(),
                SpiderSpawnData = ToWorldData(spiderSpawns[0]),
                FlowerSpawnData = ToWorldData(flowers[0])
            };

            gameData.GameDatas[catalog.LevelDataKey] = data;
            EditorUtility.SetDirty(gameData);

            Debug.Log($"Collect: записано в '{catalog.LevelDataKey}' из {catalogScenes.Count} сцен " +
                $"({string.Join(", ", catalogScenes.Select(s => s.name))}). " +
                $"CheckPoints {data.CheckPoints.Count}, Generators {data.GeneratorPoints.Count}, " +
                $"Batteries {data.BatteriesPoints.Count}, Energy {data.EnergyPoints.Count}, " +
                $"Elephant {data.ElephantPoints.Count}, Skills {data.SkillsData.Count}, " +
                "SpiderSpawn 1, Flower 1.");
        }

        private static WorldData ToWorldData(MarkerBase marker) =>
            new WorldData(marker.transform.position, marker.transform.rotation, marker.UniqueId);
    }
}
