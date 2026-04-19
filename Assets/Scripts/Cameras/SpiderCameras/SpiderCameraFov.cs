using Infastructure.Services.Defeat;
using Infastructure.Services.Registries.SpiderRegistry;
using SpiderController.StateMachine;
using UnityEngine;

namespace Cameras.SpiderCameras
{
    public class SpiderCameraFov
    {
        private readonly IDefeatWindowService _defeatWindowService;
        private readonly ISpiderCamera _spiderCamera;
        private readonly ISpiderRegistryService _spiderRegistryService;
        private StateMachineData Data => _spiderRegistryService.Spider?.StateMachineData;

        public SpiderCameraFov(
            ISpiderCamera spiderCamera,
            IDefeatWindowService defeatWindowService,
            ISpiderRegistryService spiderRegistryService)
        {
            _defeatWindowService = defeatWindowService;
            _spiderCamera = spiderCamera;
            _spiderRegistryService = spiderRegistryService;
        }


        public void Update()
        {
            if (Data == null || _defeatWindowService.IsDefeated)
                return;

            UpdateFov();
        }

        private void UpdateFov()
        {
            float targetFov =
                Data.IsGravityGunState ? 50f :
                Data.IsAimingState ? 90f : 70f;

            _spiderCamera.FieldOfView = Mathf.Lerp(
                _spiderCamera.FieldOfView,
                targetFov,
                Time.deltaTime * 5f);
        }
    }
}