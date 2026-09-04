using System.Collections.Generic;
using Infastructure.StaticData.World;
using UnityEngine;

namespace Editor.World
{
    /// <summary>
    /// Общие помощники редакторных инструментов по сценам каталога: контентные сцены
    /// (EntryScene + сцены сегментов; бутстрап, резидентный слой и сцена атмосферы сюда
    /// не входят — они не сцены каталога-контента, docs/scene-regulations.md §1,
    /// Р8 в .scratch/plans/jaunty-snuggling-bengio.md) и путь объекта в иерархии сцены.
    /// Общий источник для Collect (GameDataEditor), Actor Keys (ActorKeysWindow)
    /// и Scene Structure (SceneStructureWindow).
    /// </summary>
    public static class CatalogContentScenes
    {
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
