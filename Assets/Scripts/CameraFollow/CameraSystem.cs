using System;
using SpiderController;
using SpiderController.StateMachine;
using Unity.Cinemachine;
using UnityEngine;

namespace CameraFollow
{
    public class CameraSystem : MonoBehaviour
    {
        [SerializeField] private CameraFollower _cameraFollower;
        [SerializeField] private CinemachineInputAxisController _cinemachineInputAxisController;
        [SerializeField] private CinemachineOrbitalFollow _orbitalFollow;
        [SerializeField] private CinemachineRotationComposer _rotationComposer;
        [SerializeField] private CinemachineThirdPersonFollow _thirdPersonFollow;
        public CinemachineInputAxisController CinemachineInputAxisController => _cinemachineInputAxisController;
        public CinemachineOrbitalFollow OrbitalFollow => _orbitalFollow;
        public CinemachineRotationComposer RotationComposer => _rotationComposer;
        public CinemachineThirdPersonFollow ThirdPersonFollow => _thirdPersonFollow;


        private void Awake() =>
            LocalInitialize();

        private void LocalInitialize() =>
            _cameraFollower.Initialize(this);


        public void Initialize(Spider spider)
        {
            _cameraFollower.transform.position = spider.transform.position + new Vector3(0, 0, 20);

            _cameraFollower.SetTarget(spider.transform);
        }
    }
}