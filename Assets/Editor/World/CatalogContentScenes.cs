using System.Collections.Generic;
using Infastructure.StaticData.World;

namespace Editor.World
{
    /// <summary>
    /// Контентные сцены каталога: EntryScene + сцены сегментов. Бутстрап, резидентный слой
    /// и сцена атмосферы сюда не входят — они не сцены каталога-контента
    /// (docs/scene-regulations.md §1, Р8 в .scratch/plans/jaunty-snuggling-bengio.md).
    /// Общий источник для Collect (GameDataEditor) и Actor Keys (ActorKeysWindow).
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
    }
}
