using Infastructure.Services.CameraProvider;
using Infastructure.StaticData.Spider;
using Infastructure.StaticData.StaticDataService;
using Unity.Cinemachine;
using UnityEngine;
using Zenject;

namespace Infastructure.Common.StableWorlUpManagement
{
    public class StableWorldUp : MonoBehaviour, IStableWorldUp
    {
        public Transform StableWorldUpTransform => this ? transform : null;

        private SpiderStaticData SpiderStaticData => _staticDataService.SpiderStaticData;

        private IStaticDataService _staticDataService;
        private CinemachineBrain _cinemachineBrain;
        private ICameraProviderService _cameraProviderService;

        [Inject]
        public void Construct(IStaticDataService staticDataService, ICameraProviderService cameraProviderService)
        {
            _cameraProviderService = cameraProviderService;
            _staticDataService = staticDataService;
        }

        private void Awake() =>
            _cinemachineBrain = _cameraProviderService.CameraTransform.GetComponent<CinemachineBrain>();

        private void Start() =>
            _cinemachineBrain.WorldUpOverride = transform;

        public void Rotate(Quaternion targetRotation)
        {
            Vector3 targetUp = targetRotation * Vector3.up;

            Quaternion fromTo = Quaternion.FromToRotation(transform.up, targetUp);
            Quaternion finalRotation = fromTo * transform.rotation;

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                finalRotation,
                Time.deltaTime * SpiderStaticData.WorldUpSmoothRotation
            );
        }
    }
}