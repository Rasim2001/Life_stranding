using Common;
using Editor.World;
using UnityEditor;
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
                    CatalogContentScenes.IssueNewKey(marker, "Assign new actor key");
            }
        }
    }
}
