using System;
using Infastructure.Services.CutScene;
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
        [SerializeField] private MMF_Player _cameraShake;
        public CinemachineRotationComposer RotationComposer => _rotationComposer;
        public CinemachineThirdPersonFollow ThirdPersonFollow => _thirdPersonFollow;
        private MMF_CinemachineImpulse Impulse => _cameraShake.GetFeedbackOfType<MMF_CinemachineImpulse>();
        private SpiderStaticData SpiderStaticData => _staticDataService.SpiderStaticData;

        private Spider _spider;
        private IStaticDataService _staticDataService;
        private ICutSceneService _cutSceneService;

        private void Awake() =>
            LocalInitialize();

        [Inject]
        public void Construct(IStaticDataService staticDataService, ICutSceneService cutSceneService)
        {
            _cutSceneService = cutSceneService;
            _staticDataService = staticDataService;
        }

        private void LocalInitialize()
        {
            _cameraFollower.Initialize(this);
            _cutSceneService.OnCutsceneActiveChanged += CutsceneActiveChanged;
        }

        public void Initialize(Spider spider)
        {
            _spider = spider;

            _cameraFollower.transform.position = _spider.transform.position + new Vector3(0, 0, 20);
            _cameraFollower.SetTarget(_spider.transform);

            _spider.OnShakeCameraHappened += ShakeCamera;
        }

        private void OnDestroy()
        {
            _spider.OnShakeCameraHappened -= ShakeCamera;
            _cutSceneService.OnCutsceneActiveChanged -= CutsceneActiveChanged;
        }

        private void ShakeCamera(float distanceFalling)
        {
            float distanceNormalized = Mathf.InverseLerp(SpiderStaticData.MinShakeDistance,
                SpiderStaticData.MaxShakeDistance, distanceFalling);

            float force = Mathf.Lerp(SpiderStaticData.MinForceShake, SpiderStaticData.MaxForceShake,
                distanceNormalized);

            Impulse.m_ImpulseDefinition.FrequencyGain = force;
            Impulse.m_ImpulseDefinition.AmplitudeGain = force;

            _cameraShake.PlayFeedbacks();
        }

        private void CutsceneActiveChanged(bool value)
        {
            if (value == false)
                StopCutScene();
        }

        private void StopCutScene()
        {
            _thirdPersonFollow.gameObject.SetActive(false);
            _thirdPersonFollow.gameObject.SetActive(true);
        }
    }
}