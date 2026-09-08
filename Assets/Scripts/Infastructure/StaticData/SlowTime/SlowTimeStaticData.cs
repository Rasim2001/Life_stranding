using UnityEngine;

namespace Infastructure.StaticData.SlowTime
{
    [CreateAssetMenu(fileName = "SlowTimeData", menuName = "StaticData/SlowTimeData")]
    public class SlowTimeStaticData : ScriptableObject
    {
        public float TargetTimeScale = 0.01f;
        public float LerpDuration = 0.5f;
        public float Duration = 1f;
        public AnimationCurve EnterCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));
    }
}
