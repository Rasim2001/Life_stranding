using Common;
using Infastructure.Services.PlatformObjects;
using Infastructure.Services.Window;
using PickupObjects.PickUpOnPlatform.FlowerManagement;
using UnityEngine;
using Zenject;

namespace SpiderController.TriggerChecker
{
    public class WinChecker : MonoBehaviour
    {
        [SerializeField] private ObserverTrigger _observerTrigger;

        private IWindowService _windowService;
        private IPlatformObjectsService _platformObjectsService;

        [Inject]
        public void Construct(IWindowService windowService, IPlatformObjectsService platformObjectsService)
        {
            _platformObjectsService = platformObjectsService;
            _windowService = windowService;
        }

        private void Start() =>
            _observerTrigger.OnTriggerEnterHappened += TriggerEnter;

        private void OnDestroy() =>
            _observerTrigger.OnTriggerEnterHappened -= TriggerEnter;

        private void TriggerEnter(Collider obj)
        {
            if (CanShow(obj))
                _windowService.OpenWinPopup();
        }

        private bool CanShow(Collider obj) =>
            obj.TryGetComponent(out Biosphere _) &&
            _platformObjectsService.HasAny<Flower>();
    }
}