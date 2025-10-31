using Common;
using Infastructure.Services.Window;
using PickupObjects.PickUpOnPlatform;
using UI;
using UnityEngine;
using Zenject;

public class CheckPoint : MonoBehaviour
{
    [SerializeField] private ObserverTrigger _observerTrigger;
    [SerializeField] private Transform _flowerPutdownPoint;
    public Vector3 FlowerPutdownPosition => _flowerPutdownPoint.position;
    public Quaternion FlowerPutdownRotation => _flowerPutdownPoint.rotation;

    private IWindowService _windowService;

    [Inject]
    public void Construct(IWindowService windowService) =>
        _windowService = windowService;

    private void Start() =>
        _observerTrigger.OnTriggerEnterHappened += TriggerEnter;

    private void OnDestroy() =>
        _observerTrigger.OnTriggerEnterHappened -= TriggerEnter;

    private void TriggerEnter(Collider obj) =>
        _windowService.OpenTaskPopup(TaskId.CheckpointDescriptionTask);
}