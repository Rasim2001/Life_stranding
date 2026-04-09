using Infastructure.StaticData.Spider;
using Infastructure.StaticData.StaticDataService;
using MoreMountains.Feedbacks;
using MoreMountains.FeedbacksForThirdParty;
using SpiderController;
using Unity.Cinemachine;
using UnityEngine;
using Zenject;

namespace CameraFollow
{
    public class CameraSystem : MonoBehaviour
    {
        [SerializeField] private CameraFollower _cameraFollower;
        [SerializeField] private CinemachineRotationComposer _rotationComposer;
        [SerializeField] private CinemachineThirdPersonFollow _thirdPersonFollow;
        [SerializeField] private CinemachineCamera _cinemachineCamera;

        [SerializeField] private MMF_Player _cameraShake;
        public CinemachineThirdPersonFollow ThirdPersonFollow => _thirdPersonFollow;
        public CinemachineCamera CinemachineCamera => _cinemachineCamera;
        private MMF_CinemachineImpulse Impulse => _cameraShake.GetFeedbackOfType<MMF_CinemachineImpulse>();
        private SpiderStaticData SpiderStaticData => _staticDataService.SpiderStaticData;

        private Spider _spider;
        private IStaticDataService _staticDataService;

        private void Awake() =>
            LocalInitialize();

        [Inject]
        public void Construct(IStaticDataService staticDataService) =>
            _staticDataService = staticDataService;

        private void LocalInitialize() =>
            _cameraFollower.Initialize(this);

        public void Initialize(Spider spider)
        {
            _spider = spider;

            _cameraFollower.transform.position = _spider.transform.position + new Vector3(0, 0, 20);
            _cameraFollower.SetTarget(_spider);

            _spider.OnShakeCameraHappened += ShakeCamera;
        }

        private void OnDestroy()
        {
            _spider.OnShakeCameraHappened -= ShakeCamera;
        }

        private void ShakeCamera(float distanceFalling)
        {
            float distanceNormalized = Mathf.InverseLerp(SpiderStaticData.MinShakeDistance,
                SpiderStaticData.MaxShakeDistance, 20);

            float force = Mathf.Lerp(SpiderStaticData.MinForceShake, SpiderStaticData.MaxForceShake,
                distanceNormalized);

            Impulse.m_ImpulseDefinition.FrequencyGain = force;
            Impulse.m_ImpulseDefinition.AmplitudeGain = force;

            _cameraShake.PlayFeedbacks();
        }
    }
}