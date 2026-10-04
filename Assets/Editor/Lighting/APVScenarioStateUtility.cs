using System;
using System.Collections.Generic;
using Common.Lights;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Editor.Lighting
{
    internal static class APVScenarioStateUtility
    {
        internal static bool CanApply => !EditorApplication.isPlayingOrWillChangePlaymode &&
            !EditorApplication.isCompiling && !EditorApplication.isUpdating;

        internal static List<BakeScenarioSet> FindLoadedSets()
        {
            List<BakeScenarioSet> sets = new List<BakeScenarioSet>();

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded || EditorSceneManager.IsPreviewScene(scene))
                    continue;

                foreach (GameObject root in scene.GetRootGameObjects())
                    sets.AddRange(root.GetComponentsInChildren<BakeScenarioSet>(true));
            }

            return sets;
        }

        internal static bool ApplyScenario(string scenarioName)
        {
            if (!CanApply)
                return false;

            ProbeReferenceVolume probeVolume = ProbeReferenceVolume.instance;
            ProbeVolumeBakingSet bakingSet = probeVolume.currentBakingSet;
            if (bakingSet == null || !ContainsScenario(bakingSet.lightingScenarios, scenarioName))
                return false;

            // Сценарий не участвует в Undo. Незапечённое имя API выбирает с сообщением в Console.
            probeVolume.lightingScenario = scenarioName;
            if (!string.Equals(probeVolume.lightingScenario, scenarioName, StringComparison.Ordinal))
                return false;

            Dictionary<GameObject, bool> states = new Dictionary<GameObject, bool>();
            foreach (BakeScenarioSet set in FindLoadedSets())
            {
                foreach (BakeScenarioSet.ScenarioState scenario in set.Scenarios)
                {
                    if (scenario == null || !string.Equals(scenario.Name, scenarioName, StringComparison.Ordinal))
                        continue;

                    CollectStates(scenario.Disable, false, states);
                    CollectStates(scenario.Enable, true, states);
                }
            }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            string undoName = "APV Scenario States: " + scenarioName;
            Undo.SetCurrentGroupName(undoName);

            try
            {
                foreach (KeyValuePair<GameObject, bool> state in states)
                {
                    GameObject target = state.Key;
                    if (target == null || target.activeSelf == state.Value)
                        continue;

                    Undo.RecordObject(target, undoName);
                    target.SetActive(state.Value);
                    if (PrefabUtility.IsPartOfPrefabInstance(target))
                        PrefabUtility.RecordPrefabInstancePropertyModifications(target);
                }
            }
            finally
            {
                Undo.CollapseUndoOperations(undoGroup);
            }

            return true;
        }

        internal static bool ContainsScenario(IReadOnlyList<string> scenarios, string scenarioName)
        {
            for (int i = 0; i < scenarios.Count; i++)
            {
                if (string.Equals(scenarios[i], scenarioName, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static void CollectStates(List<GameObject> targets, bool active,
            Dictionary<GameObject, bool> states)
        {
            if (targets == null)
                return;

            foreach (GameObject target in targets)
            {
                if (target == null || EditorUtility.IsPersistent(target))
                    continue;

                Scene scene = target.scene;
                if (!scene.IsValid() || !scene.isLoaded || EditorSceneManager.IsPreviewScene(scene))
                    continue;

                // Сначала Disable, потом Enable: последняя запись задаёт конечное состояние.
                // Сводим ссылки, чтобы каждый объект попал в Undo один раз.
                states[target] = active;
            }
        }
    }
}
