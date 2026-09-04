using System;
using System.Collections.Generic;
using System.Linq;
using Infastructure.StaticData.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Editor.World
{
    /// <summary>
    /// Общие помощники редакторных инструментов по сценам каталога: поиск WorldCatalog
    /// в проекте, контентные сцены (EntryScene + сцены сегментов; бутстрап, резидентный
    /// слой и сцена атмосферы сюда не входят — они не сцены каталога-контента,
    /// docs/scene-regulations.md §1, Р8 в .scratch/plans/jaunty-snuggling-bengio.md),
    /// аддитивное открытие/закрытие сцен с восстановлением setup и путь объекта
    /// в иерархии сцены. Общий источник для Collect (GameDataEditor), Actor Keys
    /// (ActorKeysWindow) и Scene Structure (SceneStructureWindow).
    /// </summary>
    public static class CatalogContentScenes
    {
        public static List<WorldCatalog> FindAllCatalogs()
        {
            return AssetDatabase.FindAssets("t:WorldCatalog")
                .Select(guid => AssetDatabase.LoadAssetAtPath<WorldCatalog>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(c => c != null)
                .ToList();
        }

        /// <summary>
        /// Открывает каждую сцену из <paramref name="scenePaths"/> аддитивно (если ещё не
        /// открыта), вызывает <paramref name="action"/> и закрывает за собой только то,
        /// что открыла сама. Setup сцен восстанавливается в finally.
        /// </summary>
        public static void ForEachScene(IEnumerable<string> scenePaths, Action<Scene> action)
        {
            SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                foreach (string path in scenePaths)
                {
                    Scene scene = EditorSceneManager.GetSceneByPath(path);
                    bool wasOpen = scene.IsValid() && scene.isLoaded;

                    if (!wasOpen)
                        scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

                    action(scene);

                    if (!wasOpen)
                        EditorSceneManager.CloseScene(scene, removeScene: true);
                }
            }
            finally
            {
                if (setup != null && setup.Length > 0)
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }

        public static List<string> CollectScenePaths(WorldCatalog catalog)
        {
            var paths = new List<string>();

            string entryPath = CatalogBuildScenes.GetScenePath(catalog.EntryScene);
            if (!string.IsNullOrEmpty(entryPath))
                paths.Add(entryPath);

            foreach (SegmentDefinition segment in catalog.Segments)
            {
                if (segment == null)
                    continue;

                foreach (SceneReference reference in segment.SceneReferences)
                {
                    string path = CatalogBuildScenes.GetScenePath(reference);
                    if (!string.IsNullOrEmpty(path) && !paths.Contains(path))
                        paths.Add(path);
                }
            }

            return paths;
        }

        public static string GetHierarchyPath(Transform t)
        {
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }

            return path;
        }
    }
}
