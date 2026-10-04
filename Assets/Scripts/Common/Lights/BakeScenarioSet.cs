using System;
using System.Collections.Generic;
using UnityEngine;

namespace Common.Lights
{
    /// <summary>Данные для Editor-инструмента. Размещается на объекте с тегом EditorOnly.</summary>
    public sealed class BakeScenarioSet : MonoBehaviour
    {
        [SerializeField] private List<ScenarioState> _scenarios = new List<ScenarioState>();

        public IReadOnlyList<ScenarioState> Scenarios => _scenarios;

        [Serializable]
        public sealed class ScenarioState
        {
            public string Name = string.Empty;
            public List<GameObject> Enable = new List<GameObject>();
            public List<GameObject> Disable = new List<GameObject>();
        }
    }
}
