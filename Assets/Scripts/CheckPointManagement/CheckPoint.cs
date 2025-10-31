using System;
using Common;
using Cysharp.Threading.Tasks;
using Infastructure.Services.Window;
using UI;
using UnityEngine;
using Zenject;

namespace CheckPointManagement
{
    public class CheckPoint : MonoBehaviour
    {
        [SerializeField] private ObserverTrigger _observerTrigger;
        [SerializeField] private Transform _flowerPutdownPoint;
        public Vector3 FlowerPutdownPosition => _flowerPutdownPoint.position;
        public Quaternion FlowerPutdownRotation => _flowerPutdownPoint.rotation;

        public bool IsReady { get; private set; }

        private IWindowService _windowService;

        [Inject]
        public void Construct(IWindowService windowService) =>
            _windowService = windowService;

        private void Start() =>
            _observerTrigger.OnTriggerEnterHappened += TriggerEnter;

        private void OnDestroy() =>
            _observerTrigger.OnTriggerEnterHappened -= TriggerEnter;

        public void StartFlowerPutdown() =>
            StartFlowerPutdownAsync().Forget();

        public void StartFlowerPickup() =>
            StartFlowerPickupAsync().Forget();

        private void TriggerEnter(Collider obj) =>
            _windowService.OpenTaskPopup(TaskId.CheckpointDescriptionTask);

        private async UniTask StartFlowerPutdownAsync()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(5));

            IsReady = true;
        }

        private async UniTask StartFlowerPickupAsync()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(5));

            IsReady = false;
        }
    }
}