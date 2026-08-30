using System.Collections.Generic;
using System.Linq;
using Infastructure.StaticData.World;
using UnityEditor;

namespace Editor.World
{
    /// <summary>
    /// Единственное место, где живёт правило «что должно быть в списке сборки»:
    /// желаемый порядок сцен по каталогу и его расхождение с EditorBuildSettings.
    /// </summary>
    public static class CatalogBuildScenes
    {
        public readonly struct Diff
        {
            public readonly List<string> ToAdd;
            public readonly List<string> ToEnable;
            public readonly List<string> ToDisable;
            public readonly bool ReorderNeeded;
            public readonly List<string> Blockers;

            public Diff(List<string> toAdd, List<string> toEnable, List<string> toDisable,
                bool reorderNeeded, List<string> blockers)
            {
                ToAdd = toAdd;
                ToEnable = toEnable;
                ToDisable = toDisable;
                ReorderNeeded = reorderNeeded;
                Blockers = blockers;
            }

            public bool InSync => Blockers.Count == 0 && ToAdd.Count == 0 && ToEnable.Count == 0 &&
                ToDisable.Count == 0 && !ReorderNeeded;
        }

        public static List<string> BuildDesiredPaths(TowerCatalog catalog)
        {
            var paths = new List<string>();

            void AddPath(SceneReference reference)
            {
                string path = GetScenePath(reference);
                if (!string.IsNullOrEmpty(path) && !paths.Contains(path))
                    paths.Add(path);
            }

            AddPath(catalog.BootstrapScene);

            if (catalog.ResidentScenes != null)
                foreach (SceneReference reference in catalog.ResidentScenes)
                    AddPath(reference);

            AddPath(catalog.EntryScene);

            foreach (SegmentDefinition segment in catalog.Segments)
            {
                if (segment == null)
                    continue;

                foreach (SceneReference reference in segment.SceneReferences)
                    AddPath(reference);
            }

            return paths;
        }

        public static Diff Compare(TowerCatalog catalog)
        {
            var blockers = new List<string>();

            if (catalog == null)
            {
                blockers.Add("no TowerCatalog assigned");
                return new Diff(new List<string>(), new List<string>(), new List<string>(), false, blockers);
            }

            string bootstrapPath = GetScenePath(catalog.BootstrapScene);
            if (string.IsNullOrEmpty(bootstrapPath))
                blockers.Add("no bootstrap scene");

            string entryPath = GetScenePath(catalog.EntryScene);
            if (string.IsNullOrEmpty(entryPath))
                blockers.Add("no entry scene");

            if (blockers.Count > 0)
                return new Diff(new List<string>(), new List<string>(), new List<string>(), false, blockers);

            List<string> desired = BuildDesiredPaths(catalog);
            EditorBuildSettingsScene[] current = EditorBuildSettings.scenes;

            var currentByPath = new Dictionary<string, EditorBuildSettingsScene>();
            foreach (EditorBuildSettingsScene scene in current)
                currentByPath[scene.path] = scene;

            var toAdd = new List<string>();
            var toEnable = new List<string>();
            var toDisable = new List<string>();

            foreach (string path in desired)
            {
                if (!currentByPath.TryGetValue(path, out EditorBuildSettingsScene scene))
                    toAdd.Add(path);
                else if (!scene.enabled)
                    toEnable.Add(path);
            }

            foreach (EditorBuildSettingsScene scene in current)
            {
                if (scene.enabled && !desired.Contains(scene.path))
                    toDisable.Add(scene.path);
            }

            var desiredSet = new HashSet<string>(desired);
            List<string> currentEnabledOrder = current.Where(s => s.enabled && desiredSet.Contains(s.path))
                .Select(s => s.path).ToList();
            bool reorderNeeded = toAdd.Count == 0 && toEnable.Count == 0 &&
                !currentEnabledOrder.SequenceEqual(desired);

            return new Diff(toAdd, toEnable, toDisable, reorderNeeded, blockers);
        }

        public static bool Apply(TowerCatalog catalog)
        {
            Diff diff = Compare(catalog);
            if (diff.Blockers.Count > 0)
                return false;

            List<string> desired = BuildDesiredPaths(catalog);
            var desiredSet = new HashSet<string>(desired);

            var scenes = new List<EditorBuildSettingsScene>();
            foreach (string path in desired)
                scenes.Add(new EditorBuildSettingsScene(path, true));

            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (desiredSet.Contains(scene.path))
                    continue;

                if (!System.IO.File.Exists(scene.path))
                    continue;

                scenes.Add(new EditorBuildSettingsScene(scene.path, false));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
            return true;
        }

        public static string DescribeRowState(string path, Diff diff)
        {
            if (diff.ToAdd.Contains(path))
                return "absent → add";
            if (diff.ToEnable.Contains(path))
                return "disabled → enable";
            if (diff.ToDisable.Contains(path))
                return "enabled → disable";

            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.path == path)
                    return scene.enabled ? "enabled" : "disabled";
            }

            return "absent";
        }

        internal static string GetScenePath(SceneReference reference) =>
            reference != null && reference.SceneAsset != null
                ? AssetDatabase.GetAssetPath(reference.SceneAsset)
                : null;
    }
}
