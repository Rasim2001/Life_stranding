using Infastructure.StaticData.World;
using UnityEditor;

namespace Editor.World
{
    [CustomEditor(typeof(SegmentBakedData))]
    public class SegmentBakedDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("Сгенерировано. Правь SegmentBounds в сцене.", MessageType.Info);

            using (new EditorGUI.DisabledScope(true))
                DrawDefaultInspector();
        }
    }
}
