using UnityEngine;

namespace Infastructure.StaticData.LastChance
{
    [CreateAssetMenu(fileName = "LastChanceData", menuName = "StaticData/LastChanceData")]
    public class LastChanceStaticData : ScriptableObject
    {
        public Sprite SelectedSprite;
        public Sprite DeselectedSprite;

        public float ShrinkingDuration = 0.5f;
        public float PressWaitTime = 0.1f;
        public int AllSaveAttempts = 10;
        public int SafeAttempts = 3;
    }
}