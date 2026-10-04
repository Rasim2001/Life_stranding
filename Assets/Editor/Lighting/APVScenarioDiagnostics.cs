using System;
using System.Collections.Generic;
using Common.Lights;
using UnityEngine.Rendering;

namespace Editor.Lighting
{
    internal static class APVScenarioDiagnostics
    {
        internal sealed class Warning
        {
            internal string Message { get; }
            internal BakeScenarioSet Source { get; }

            internal Warning(string message, BakeScenarioSet source = null)
            {
                Message = message;
                Source = source;
            }
        }

        internal static List<Warning> Collect(ProbeVolumeBakingSet bakingSet,
            IReadOnlyList<BakeScenarioSet> sets)
        {
            List<Warning> warnings = new List<Warning>();
            if (bakingSet == null)
                return warnings;

            HashSet<string> described = new HashSet<string>(StringComparer.Ordinal);
            foreach (BakeScenarioSet set in sets)
            {
                if (set == null || set.Scenarios == null)
                    continue;

                foreach (BakeScenarioSet.ScenarioState scenario in set.Scenarios)
                {
                    if (scenario == null)
                        continue;

                    described.Add(scenario.Name);
                    if (APVScenarioStateUtility.ContainsScenario(bakingSet.lightingScenarios, scenario.Name))
                        continue;

                    string name = string.IsNullOrEmpty(scenario.Name) ? "(невыбран)" : scenario.Name;
                    warnings.Add(new Warning($"Имя сценария «{name}» отсутствует в Baking Set. " +
                        $"Источник: {set.gameObject.name}, сцена {set.gameObject.scene.name}.", set));
                }
            }

            foreach (string scenarioName in bakingSet.lightingScenarios)
            {
                if (!described.Contains(scenarioName))
                    warnings.Add(new Warning($"Сценарий «{scenarioName}» не описан ни одним BakeScenarioSet."));

                // Наличие записи запечки не проверяет целостность файлов и качество света.
                if (!bakingSet.HasBakedData(scenarioName))
                    warnings.Add(new Warning($"Сценарий «{scenarioName}» не запечён."));
            }

            return warnings;
        }
    }
}
