using Infastructure.Factories;
using Infastructure.Services.Registries.SpiderRegistry;
using Infastructure.StaticData.Spider;
using Infastructure.StaticData.StaticDataService;
using MoreMountains.Feedbacks;
using MoreMountains.FeedbacksForThirdParty;
using SpiderController.StateMachine;
using Unity.Cinemachine;
using UnityEngine;
using Zenject;

namespace Cameras.SpiderCameras
{
    public class SpiderCamera : MonoBehaviour, ISpiderCamera
    {
        [SerializeField] Transform _pivot;

        [SerializeField] private CinemachineRotationComposer _rotationComposer;
        [SerializeField] private CinemachineThirdPersonFollow _thirdPersonFollow;
        [SerializeField] private CinemachineCamera _cinemachineCamera;
        [SerializeField] private MMF_Player _cameraShake;

        public Vector3 ShoulderOffset
        {
            get => _thirdPersonFollow.ShoulderOffset;
            set => _thirdPersonFollow.ShoulderOffset = value;
        }

        public float FieldOfView
        {
            get => _cinemachineCamera.Lens.FieldOfView;
            set => _cinemachineCamera.Lens.FieldOfView = value;
        }

        public float Distance
        {
            get => _thirdPersonFollow.CameraDistance;
            set => _thirdPersonFollow.CameraDistance = value;
        }

        private MMF_CinemachineImpulse Impulse =>
            _cameraShake.GetFeedbackOfType<MMF_CinemachineImpulse>();

        private IStaticDataService _staticData;
        private IDiFactory _diFactory;

        private SpiderCameraFollower _follower;
        private SpiderCameraFov _fov;
        private SpiderCameraOrbit _orbit;
        private SpiderCameraDistance _distance;


        [Inject]
        public void Construct(IStaticDataService staticData, IDiFactory diFactory)
        {
            _diFactory = diFactory;
            _staticData = staticData;
        }

        public void Initialize()
        {
            _follower = _diFactory.Create<SpiderCameraFollower>(_pivot);
            _fov = _diFactory.Create<SpiderCameraFov>(this);
            _distance = _diFactory.Create<SpiderCameraDistance>(this);

            _orbit = _diFactory.Create<SpiderCameraOrbit>(this, _pivot);
            _orbit.Initialize();
        }


        public void ShakeCamera(float distanceFalling)
        {
            SpiderStaticData data = _staticData.SpiderStaticData;

            float distanceNormalized = Mathf.InverseLerp(
                data.MinShakeDistance,
                data.MaxShakeDistance,
                distanceFalling);

            float force = Mathf.Lerp(data.MinForceShake, data.MaxForceShake, distanceNormalized);

            Impulse.m_ImpulseDefinition.FrequencyGain = force;
            Impulse.m_ImpulseDefinition.AmplitudeGain = force;

            _cameraShake.PlayFeedbacks();
        }

        private void OnDestroy() =>
            _orbit.Destroy();

        private void Update()
        {
            if (_follower == null || _fov == null || _orbit == null)
                return;

            _follower.Update();
            _fov.Update();
            _orbit.Update();
            _distance.Update();
        }

        private void FixedUpdate()
        {
            if (_follower == null || _fov == null || _orbit == null)
                return;

            _follower.FixedUpdate();
        }
    }
}