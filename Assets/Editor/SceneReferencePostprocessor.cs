using System.Linq;
using Infastructure.StaticData.World;
using UnityEditor;

namespace Editor
{
    /// <summary>
    /// Пересинхронизирует производное имя SceneReference после переименования
    /// переноса или удаления .unity-ассета. Без этого шва имя протухает молча: у удалённой
    /// сцены ссылка на ассет обнуляется, а имя остаётся, и IsValid продолжает
    /// говорить «всё в порядке» до падения в рантайме.
    /// </summary>
    public class SceneReferencePostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            bool touchedScene = importedAssets
                .Concat(movedAssets)
                .Concat(deletedAssets)
                .Any(path => path.EndsWith(".unity"));
            if (!touchedScene)
                return;

            foreach (string guid in AssetDatabase.FindAssets("t:WorldCatalog t:SegmentDefinition"))
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
