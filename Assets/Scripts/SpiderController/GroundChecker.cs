using System.Linq;
using SpiderController.SpiderMove;
using UnityEngine;

namespace SpiderController
{
    public class GroundChecker : MonoBehaviour
    {
        [SerializeField] private LegRaycast[] _legRaycasts;
        public bool IsTouches { get; private set; }

        public void SetGroundLegState()
        {
            foreach (LegRaycast legRaycast in _legRaycasts)
                legRaycast.SetGroundState();
        }

        public void SetAirbornLegState()
        {
            foreach (LegRaycast legRaycast in _legRaycasts)
                legRaycast.SetAirbornState();
        }

        private void Update() =>
            IsTouches = _legRaycasts.Any(x => x.IsGrounded);
    }
}