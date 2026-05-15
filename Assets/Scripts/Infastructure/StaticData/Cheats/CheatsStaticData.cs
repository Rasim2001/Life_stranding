using UnityEngine;

namespace Infastructure.StaticData.Cheats
{
    [CreateAssetMenu(fileName = "Cheats", menuName = "StaticData/Cheats")]
    public class CheatsStaticData : ScriptableObject
    {
        public bool TasksPopupEnabled;
        public bool ProductsPopupEnabled;
    }
}