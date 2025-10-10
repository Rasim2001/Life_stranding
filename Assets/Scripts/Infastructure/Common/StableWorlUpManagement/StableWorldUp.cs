using Infastructure.StaticData.Spider;
using Infastructure.StaticData.StaticDataService;
using Unity.Cinemachine;
using UnityEngine;
using Zenject;

namespace Infastructure.Common.StableWorlUpManagement
{
    public class StableWorldUp : MonoBehaviour, IStableWorldUp
    {
        [SerializeField] private bool _isActive = true;

        private SpiderStaticData SpiderStaticData => _staticDataService.SpiderStaticData;

        private IStaticDataService _staticDataService;
        private CinemachineBrain _cinemachineBrain;

        [Inject]
        public void Construct(IStaticDataService staticDataService) =>
            _staticDataService = staticDataService;

        private void Awake() =>
            _cinemachineBrain = Camera.main.GetComponent<CinemachineBrain>();

        private void Start() =>
            _cinemachineBrain.WorldUpOverride = transform;

        public void Rotate(Quaternion targetRotation)
        {
            if (!_isActive)
                return;

            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * SpiderStaticData.WorldUpSmoothRotation
            );
        }
    }
}