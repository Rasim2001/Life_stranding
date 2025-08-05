using Common;
using Unity.Cinemachine;
using UnityEngine;

namespace _2
{
    public class SpiderCanvasUI : MonoBehaviour
    {
        private RotateToCamera _rotateToCamera;

        private void Awake() =>
            _rotateToCamera = new RotateToCamera(Camera.main);

        private void Start() =>
            CinemachineCore.CameraUpdatedEvent.AddListener(UpdateRotation);

        private void OnDestroy() =>
            CinemachineCore.CameraUpdatedEvent.RemoveListener(UpdateRotation);

        private void UpdateRotation(CinemachineBrain _) =>
            _rotateToCamera.UpdateRotation(transform);
    }
}