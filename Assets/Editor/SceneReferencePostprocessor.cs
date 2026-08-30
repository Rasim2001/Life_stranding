using System.Linq;
using Infastructure.StaticData.World;
using UnityEditor;

namespace Editor
{
    /// <summary>
    /// Пересинхронизирует производное имя SceneReference после переименования
    /// или переноса .unity-ассета. Без этого шва имя протухает молча.
    /// </summary>
    public class SceneReferencePostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            bool touchedScene = importedAssets.Concat(movedAssets).Any(path => path.EndsWith(".unity"));
            if (!touchedScene)
                return;

            foreach (string guid in AssetDatabase.FindAssets("t:TowerCatalog t:SegmentDefinition"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var owner = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) as ISceneReferenceOwner;
                if (owner == null)
                    continue;

                bool changed = owner.SceneReferences
                    .Where(r => r != null)
                    .Aggregate(false, (acc, r) => r.SyncFromAsset() || acc);

                if (changed)
                {
                    EditorUtility.SetDirty((UnityEngine.Object)owner);
                    AssetDatabase.SaveAssetIfDirty((UnityEngine.Object)owner);
                }
            }
        }
    }
}
