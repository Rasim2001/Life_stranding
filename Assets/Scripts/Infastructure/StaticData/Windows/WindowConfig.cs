using System;
using UnityEngine;

namespace Infastructure.StaticData.Windows
{
    [Serializable]
    public class WindowConfig
    {
        public WindowId WindowId;
        public GameObject Prefab;
    }
}