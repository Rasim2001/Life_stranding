using Unity.Cinemachine;
using UnityEngine;

namespace CameraFollow
{
    public class CameraSystem : MonoBehaviour
    {
        [SerializeField] private CameraFollower _cameraFollower;
        [SerializeField] private CinemachineInputAxisController _cinemachineInputAxisController;
        [SerializeField] private CinemachineOrbitalFollow _orbitalFollow;
        public CinemachineInputAxisController CinemachineInputAxisController => _cinemachineInputAxisController;
        public CinemachineOrbitalFollow OrbitalFollow => _orbitalFollow;

        private void Awake() =>
            Initialize();

        private void Initialize() =>
            _cameraFollower.Initialize(this);

        public void SetTarget(Transform spiderTransform)
        {
            _cameraFollower.transform.position = spiderTransform.position;
            _cameraFollower.SetTarget(spiderTransform);
        }
    }
}