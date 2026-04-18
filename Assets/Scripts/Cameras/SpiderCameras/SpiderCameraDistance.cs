using Infastructure.Services.Defeat;
using Infastructure.Services.Registries.SpiderRegistry;
using SpiderController.StateMachine;
using UnityEngine;

namespace Cameras.SpiderCameras
{
    public class SpiderCameraDistance
    {
        private readonly IDefeatWindowService _defeatWindowService;
        private readonly ISpiderCamera _spiderCamera;
        private readonly ISpiderRegistryService _spiderRegistryService;
        private StateMachineData Data => _spiderRegistryService.Spider?.StateMachineData;

        public SpiderCameraDistance(
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

            UpdateDistance();
        }

        private void UpdateDistance()
        {
            float targetFov =
                Data.CanRecordFootprints ? -0.33f : 5;

            _spiderCamera.Distance = Mathf.Lerp(
                _spiderCamera.Distance,
                targetFov,
                Time.deltaTime * 5f);
        }
    }
}