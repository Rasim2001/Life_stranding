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
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * SpiderStaticData.WorldUpSmoothRotation
            );
        }
    }
}