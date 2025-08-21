using System.Linq;
using SpiderController.SpiderMove;
using UnityEngine;

namespace SpiderController
{
    public class GroundChecker : MonoBehaviour
    {
        [SerializeField] private LayerMask _groundLayer;
        [SerializeField] private LegRaycast[] _legRaycasts;
        public bool IsTouchesWithLegs { get; private set; }

        private void Update() =>
            IsTouchesWithLegs = _legRaycasts.Any(x => x.IsGrounded);
    }
}