using Infastructure.Common.StableWorlUpManagement;
using Infastructure.Services.Defeat;
using Infastructure.Services.Registries.SpiderRegistry;
using Infastructure.StaticData.StaticDataService;
using SpiderController;
using UnityEngine;

namespace Cameras.SpiderCameras
{
    public class SpiderCameraFollower
    {
        private readonly IStableWorldUp _stableWorldUp;
        private readonly IStaticDataService _staticDataService;
        private readonly IDefeatWindowService _defeatWindowService;
        private readonly ISpiderRegistryService _spiderRegistryService;
        private readonly Transform _pivot;
        private Spider Spider => _spiderRegistryService.Spider;

        private Vector3 _velocity;

        public SpiderCameraFollower(
            IStableWorldUp stableWorldUp,
            IStaticDataService staticDataService,
            IDefeatWindowService defeatWindowService,
            ISpiderRegistryService spiderRegistryService,
            Transform pivot)
        {
            _spiderRegistryService = spiderRegistryService;
            _stableWorldUp = stableWorldUp;
            _staticDataService = staticDataService;
            _defeatWindowService = defeatWindowService;
            _pivot = pivot;
        }

        public void Snap()
        {
            _pivot.position = Spider.transform.position;
            _velocity = Vector3.zero;
        }

        public void Update()
        {
            if (Spider == null || _defeatWindowService.IsDefeated)
                return;

            WorldUpRotate();
        }

        public void FixedUpdate()
        {
            if (Spider == null || _defeatWindowService.IsDefeated)
                return;

            MoveToTarget();
        }

        private void MoveToTarget()
        {
            _pivot.position = Vector3.SmoothDamp(
                _pivot.position,
                Spider.transform.position,
                ref _velocity,
                _staticDataService.SpiderStaticData.SmoothTime,
                Mathf.Infinity,
                Time.fixedDeltaTime);
        }

        // IsTouchesWithLegs is the same grounded signal the state machine transitions on, so the
        // horizon commits on exactly the surfaces the spider is considered to be standing on.
        private void WorldUpRotate() =>
            _stableWorldUp.Rotate(
                Spider.transform.rotation,
                Spider.StateContext.GroundChecker.IsTouchesWithLegs);
    }
}