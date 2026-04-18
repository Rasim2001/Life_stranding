using Cameras.SpiderCameras;
using Infastructure.Services.Pool;
using Infastructure.Services.Registries.SpiderRegistry;
using PickupObjects.Teleports;
using SpiderController;
using UI.Curtain;
using UnityEngine;

namespace Infastructure.Services.Teleports
{
    public class TeleportService : ITeleportService
    {
        private readonly ISpiderRegistryService _spiderRegistry;
        private readonly ISpiderCamera _spiderCamera;
        private readonly IPoolObjects<Teleport> _teleportPools;
        private readonly ICurtainRoot _curtainRoot;

        private Teleport _slotA;
        private Teleport _slotB;

        public bool HasBothTeleports => _slotA != null && _slotB != null;

        public TeleportService(ISpiderRegistryService spiderRegistry, ISpiderCamera spiderCamera, IPoolObjects<Teleport> teleportPools,
            ICurtainRoot curtainRoot)
        {
            _spiderRegistry = spiderRegistry;
            _spiderCamera = spiderCamera;
            _teleportPools = teleportPools;
            _curtainRoot = curtainRoot;
        }

        public Teleport SpawnNewTeleport(Vector3 position)
        {
            if (_slotA != null)
                _teleportPools.ReturnObjectToPool(_slotA);

            _slotA = _slotB;
            _slotB = GetTeleport(position);

            _slotA?.SetOtherTeleport(_slotB);
            _slotB?.SetOtherTeleport(_slotA);

            return _slotB;
        }

        public void TryTeleportSpider(Teleport from)
        {
            if (!HasBothTeleports)
                return;

            _curtainRoot.FandeIn(2);

            Teleport target = from == _slotA ? _slotB : _slotA;
            target.BlockEntry();

            Spider spider = _spiderRegistry.Spider;

            spider.transform.position = target.transform.position + target.transform.forward * 0.5f;
            spider.transform.rotation = target.transform.rotation;
            spider.StateContext.BodyOrientation.SnapOnTeleport();

            _spiderCamera.SnapToTarget();
            _spiderCamera.AlignToSpider();
        }


        private Teleport GetTeleport(Vector3 position)
        {
            Teleport teleport = _teleportPools.GetObjectFromPool();

            Vector3 direction = _spiderRegistry.Spider.transform.position - position;
            float yAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

            teleport.Initialize(position, yAngle);

            return teleport;
        }
    }
}