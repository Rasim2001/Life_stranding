using UnityEngine;

namespace Infastructure.StaticData.GlobalWater
{
    [CreateAssetMenu(fileName = "WaterData", menuName = "StaticData/WaterData")]
    public class WaterStaticData : ScriptableObject
    {
        public GameObject WaterSplashPrefab;

        public float FarSpeed;
        public float NearSpeed;
        public float DistanceToSwitchSpeed;
        public float DistanceBetweenSpiderToDefeat;
    }
}