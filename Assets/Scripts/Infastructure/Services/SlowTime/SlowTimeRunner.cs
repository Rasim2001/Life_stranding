using MoreMountains.Feedbacks;
using UnityEngine;

namespace Infastructure.Services.SlowTime
{
    public class SlowTimeRunner : MonoBehaviour, ISlowTimeRunner
    {
        [SerializeField] private MMF_Player _feedbackPlayer;

        private bool _isRunning;

        public void SlowDown()
        {
            _isRunning = true;

            _feedbackPlayer.PlayFeedbacks();
        }

        public void StopSlowDown()
        {
            _isRunning = false;

            _feedbackPlayer.StopFeedbacks();
        }

        public bool IsRunning() => _isRunning;
    }
}