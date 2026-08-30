using Common;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(MarkerUniqueId))]
    public class MarkerUniqueIdEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var marker = (MarkerUniqueId)target;

            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.TextField("Unique Id", marker.UniqueId);

            if (GUILayout.Button("Выдать новый ключ"))
            {
                bool confirmed = EditorUtility.DisplayDialog(
                    "Выдать новый ключ",
                    "Память этого экземпляра будет потеряна: сохранённый прогресс, " +
                    "привязанный к текущему ключу, станет недостижим. Продолжить?",
                    "Выдать новый ключ",
                    "Отмена");

                if (confirmed)
                {
                    Undo.RecordObject(marker, "Assign new actor key");
                    marker.UniqueId = System.Guid.NewGuid().ToString();
                    PrefabUtility.RecordPrefabInstancePropertyModifications(marker);
                    EditorUtility.SetDirty(marker);
                    EditorSceneManager.MarkSceneDirty(marker.gameObject.scene);
                }
            }
        }
    }
}
