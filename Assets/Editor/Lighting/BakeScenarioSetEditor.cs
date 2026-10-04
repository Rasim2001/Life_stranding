using System;
using System.Collections.Generic;
using Common.Lights;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Editor.Lighting
{
    [CustomEditor(typeof(BakeScenarioSet))]
    public sealed class BakeScenarioSetEditor : UnityEditor.Editor
    {
        private SerializedProperty _scenarios;

        private void OnEnable() => _scenarios = serializedObject.FindProperty("_scenarios");

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            ProbeVolumeBakingSet bakingSet = ProbeReferenceVolume.instance.currentBakingSet;
            if (bakingSet == null)
                EditorGUILayout.HelpBox("Baking Set не загружен. Сохранённые имена оставлены без изменений.",
                    MessageType.Warning);

            using (new EditorGUI.DisabledScope(!APVScenarioStateUtility.CanApply))
            {
                for (int i = 0; i < _scenarios.arraySize; i++)
                {
                    SerializedProperty scenario = _scenarios.GetArrayElementAtIndex(i);
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        DrawScenarioName(scenario.FindPropertyRelative("Name"), bakingSet);
                        EditorGUILayout.PropertyField(scenario.FindPropertyRelative("Enable"),
                            new GUIContent("Включить"), true);
                        EditorGUILayout.PropertyField(scenario.FindPropertyRelative("Disable"),
                            new GUIContent("Выключить"), true);

                        if (GUILayout.Button("Удалить сценарий"))
                        {
                            _scenarios.DeleteArrayElementAtIndex(i);
                            break;
                        }
                    }
                }

                if (GUILayout.Button("Добавить сценарий"))
                {
                    int index = _scenarios.arraySize;
                    _scenarios.InsertArrayElementAtIndex(index);
                    SerializedProperty added = _scenarios.GetArrayElementAtIndex(index);
                    added.FindPropertyRelative("Name").stringValue = string.Empty;
                    added.FindPropertyRelative("Enable").ClearArray();
                    added.FindPropertyRelative("Disable").ClearArray();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawScenarioName(SerializedProperty name, ProbeVolumeBakingSet bakingSet)
        {
            string savedName = name.stringValue;
            string savedLabel = string.IsNullOrEmpty(savedName) ? "(не выбран)" : savedName;
            if (bakingSet == null)
            {
                EditorGUILayout.LabelField("Сценарий", savedLabel);
                return;
            }

            List<string> names = new List<string>(bakingSet.lightingScenarios);
            int selected = names.FindIndex(value => string.Equals(value, savedName, StringComparison.Ordinal));
            bool missing = selected < 0;
            if (missing)
            {
                names.Insert(0, savedName);
                selected = 0;
                EditorGUILayout.HelpBox(string.IsNullOrEmpty(savedName)
                    ? "Выберите сценарий из текущего Baking Set."
                    : "Сценария «" + savedName + "» нет в текущем Baking Set. Имя сохранено.",
                    MessageType.Warning);
            }

            string[] labels = names.ToArray();
            if (missing)
                labels[0] = savedLabel;

            EditorGUI.BeginChangeCheck();
            int updated = EditorGUILayout.Popup("Сценарий", selected, labels);
            if (EditorGUI.EndChangeCheck())
                name.stringValue = names[updated];
        }
    }
}
