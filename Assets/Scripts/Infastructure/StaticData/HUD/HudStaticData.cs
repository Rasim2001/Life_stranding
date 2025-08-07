using UnityEngine;

namespace Infastructure.StaticData.HUD
{
    [CreateAssetMenu(fileName = "HudData", menuName = "StaticData/HudData")]
    public class HudStaticData : ScriptableObject
    {
        public RectTransform ArrowUIPrefab;
    }
}