using MoreMountains.Feedbacks;
using UnityEngine;

namespace Infastructure.Services.SlowTime
{
    public class SlowTimeRunner : MonoBehaviour, ISlowTimeRunner
    {
        private MMF_Player _feedbackPlayer;

        private void Awake() =>
            _feedbackPlayer = GetComponent<MMF_Player>();

        public void SlowDown() =>
            _feedbackPlayer.PlayFeedbacks();

        public void StopSlowDown() =>
            _feedbackPlayer.StopFeedbacks();
    }
}