using Infastructure.Services.Teleports;
using SpiderController;
using UnityEngine;
using Zenject;

namespace PickupObjects.Teleports
{
    public class Teleport : MonoBehaviour
    {
        private ITeleportService _teleportOverlayState;

        [Inject]
        public void Construct(ITeleportService teleportOverlayState) =>
            _teleportOverlayState = teleportOverlayState;

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<Spider>(out _))
                _teleportOverlayState.TryTeleportSpider(this);
        }
    }
}