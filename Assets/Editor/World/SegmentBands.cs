using System.Collections.Generic;
using System.IO;
using Infastructure.StaticData.World;
using Infastructure.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Editor.World
{
    /// <summary>
    /// Запекание высотных полос сегментов из <see cref="SegmentBounds"/> в сцене в
    /// <see cref="SegmentBakedData"/>-ассеты, и чтение уже запечённых полос без открытия
    /// сцен (тикет 08, scene-architecture.md §7.6). Сканирование сцен и восстановление
    /// набора открытых сцен повторяют ActorKeysWindow.
    /// </summary>
    public static class SegmentBands
    {
        private const string SegmentsFolderPath = "Assets/Settings/World/Segments";
        private const string GeneratedFolderPath = "Assets/Settings/World/Segments/Generated";

        public readonly struct SyncResult
        {
            public readonly SegmentDefinition Segment;
            public readonly string SceneName;
            public readonly bool Success;
            public readonly string Message;

            public SyncResult(SegmentDefinition segment, string sceneName, bool success, string message)
            {
                Segment = segment;
                SceneName = sceneName;
                Success = success;
                Message = message;
            }
        }

        public static List<SyncResult> Sync(WorldCatalog catalog)
        {
            var results = new List<SyncResult>();
            if (catalog == null)
                return results;

            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                if (EditorSceneManager.GetSceneAt(i).isDirty)
                {
                    results.Add(new SyncResult(null, null, false, "Сохрани сцены и запусти снова."));
                    return results;
                }
            }

            EnsureGeneratedFolder();

            SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                foreach (SegmentDefinition segment in catalog.Segments)
                {
                    if (segment == null)
                        continue;

                    results.Add(SyncSegment(segment));
                }
            }
            finally
            {
                if (setup != null && setup.Length > 0)
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
            }

            AssetDatabase.SaveAssets();
            return results;
        }

        private static SyncResult SyncSegment(SegmentDefinition segment)
        {
            string path = CatalogBuildScenes.GetScenePath(segment.Scene);
            if (string.IsNullOrEmpty(path))
                return new SyncResult(segment, null, false, "Нет сцены сегмента.");

            string sceneName = Path.GetFileNameWithoutExtension(path);
            Scene scene = EditorSceneManager.GetSceneByPath(path);
            bool wasOpen = scene.IsValid() && scene.isLoaded;

            if (!wasOpen)
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

            try
            {
                var found = new List<SegmentBounds>();
                foreach (GameObject root in scene.GetRootGameObjects())
                    found.AddRange(root.GetComponentsInChildren<SegmentBounds>(true));

                if (found.Count == 0)
                    return new SyncResult(segment, sceneName, false, "В сцене нет SegmentBounds.");

                if (found.Count > 1)
                    return new SyncResult(segment, sceneName, false,
                        $"В сцене {found.Count} компонентов SegmentBounds, нужен один.");

                SegmentBounds bounds = found[0];
                if (!bounds.IsValid)
                    return new SyncResult(segment, sceneName, false,
                        $"TopY ({bounds.TopY:0.##}) ≤ BottomY ({bounds.BottomY:0.##}).");

                string assetPath = $"{GeneratedFolderPath}/DATA_SegmentBaked_{sceneName}.asset";
                SegmentBakedData baked = segment.Baked;
                if (baked == null)
                    baked = AssetDatabase.LoadAssetAtPath<SegmentBakedData>(assetPath);

                if (baked == null)
                {
                    baked = ScriptableObject.CreateInstance<SegmentBakedData>();
                    AssetDatabase.CreateAsset(baked, assetPath);
                }

                baked.SetBand(bounds.BottomY, bounds.TopY, sceneName);
                EditorUtility.SetDirty(baked);

                if (segment.Baked != baked)
                {
                    Undo.RecordObject(segment, "Sync Segment Band");
                    segment.SetBaked(baked);
                    EditorUtility.SetDirty(segment);
                }

                return new SyncResult(segment, sceneName, true, $"[{bounds.BottomY:0.##}, {bounds.TopY:0.##}]");
            }
            finally
            {
                if (!wasOpen)
                    EditorSceneManager.CloseScene(scene, removeScene: true);
            }
        }

        private static void EnsureGeneratedFolder()
        {
            if (!AssetDatabase.IsValidFolder(SegmentsFolderPath))
                AssetDatabase.CreateFolder("Assets/Settings/World", "Segments");

            if (!AssetDatabase.IsValidFolder(GeneratedFolderPath))
                AssetDatabase.CreateFolder(SegmentsFolderPath, "Generated");
        }

        /// <summary>Полосы из уже сериализованной ссылки SegmentDefinition.Baked. Сцены не открывает.</summary>
        public static Dictionary<SegmentDefinition, SegmentBakedData> ReadBands(WorldCatalog catalog)
        {
            var result = new Dictionary<SegmentDefinition, SegmentBakedData>();
            if (catalog == null)
                return result;

            foreach (SegmentDefinition segment in catalog.Segments)
            {
                if (segment == null)
                    continue;

                result[segment] = segment.Baked;
            }

            return result;
        }
    }
}
