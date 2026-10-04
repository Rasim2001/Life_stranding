using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Editor.Lighting
{
    public sealed class APVScenarioStatesWindow : EditorWindow
    {
        private Vector2 _scroll;
        private ProbeVolumeBakingSet _diagnosticsSet;
        private List<APVScenarioDiagnostics.Warning> _warnings = new List<APVScenarioDiagnostics.Warning>();
        private double _nextDiagnosticsTime;

        [MenuItem("GD Tools/Lighting/APV Scenario States")]
        public static void Open() => GetWindow<APVScenarioStatesWindow>("APV Scenario States");

        private void OnEnable() => RefreshDiagnostics(true);

        private void OnFocus() => RefreshDiagnostics(true);

        private void OnInspectorUpdate()
        {
            RefreshDiagnostics();
            Repaint();
        }

        private void RefreshDiagnostics(bool force = false)
        {
            ProbeVolumeBakingSet bakingSet = ProbeReferenceVolume.instance.currentBakingSet;
            if (!APVScenarioStateUtility.CanApply || bakingSet == null)
            {
                _warnings.Clear();
                _diagnosticsSet = null;
                return;
            }

            double now = EditorApplication.timeSinceStartup;
            if (!force && now < _nextDiagnosticsTime)
                return;

            _warnings = APVScenarioDiagnostics.Collect(bakingSet, APVScenarioStateUtility.FindLoadedSets());
            _diagnosticsSet = bakingSet;
            _nextDiagnosticsTime = now + 1.0;
        }

        private void OnGUI()
        {
            ProbeReferenceVolume probeVolume = ProbeReferenceVolume.instance;
            ProbeVolumeBakingSet bakingSet = probeVolume.currentBakingSet;
            if (bakingSet == null)
            {
                EditorGUILayout.HelpBox("Baking Set не загружен", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Baking Set", bakingSet.name);
            EditorGUILayout.LabelField("Активный сценарий", probeVolume.lightingScenario);
            EditorGUILayout.HelpBox("Ctrl+Z возвращает объекты. Активный сценарий остаётся выбранным.",
                MessageType.Info);

            if (!APVScenarioStateUtility.CanApply)
                EditorGUILayout.HelpBox("Переключение недоступно во время Play Mode, компиляции и импорта.",
                    MessageType.Info);

            using (EditorGUILayout.ScrollViewScope scroll = new EditorGUILayout.ScrollViewScope(_scroll))
            {
                _scroll = scroll.scrollPosition;
                using (new EditorGUI.DisabledScope(!APVScenarioStateUtility.CanApply))
                {
                    foreach (string scenario in bakingSet.lightingScenarios)
                    {
                        bool active = string.Equals(scenario, probeVolume.lightingScenario,
                            StringComparison.Ordinal);
                        string label = active ? scenario + " (активный)" : scenario;
                        if (GUILayout.Button(label))
                        {
                            APVScenarioStateUtility.ApplyScenario(scenario);
                            RefreshDiagnostics(true);
                        }
                    }
                }

                if (APVScenarioStateUtility.CanApply && _diagnosticsSet == bakingSet)
                {
                    foreach (APVScenarioDiagnostics.Warning warning in _warnings)
                    {
                        EditorGUILayout.HelpBox(warning.Message, MessageType.Warning);
                        if (warning.Source != null && GUILayout.Button("Показать"))
                            EditorGUIUtility.PingObject(warning.Source.gameObject);
                    }
                }
            }
        }
    }
}
