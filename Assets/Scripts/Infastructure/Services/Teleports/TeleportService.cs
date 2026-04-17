using DG.Tweening;
using Infastructure.Services.Pool;
using Infastructure.Services.Registries.SpiderRegistry;
using PickupObjects.Teleports;
using SpiderController;
using UnityEngine;

namespace Infastructure.Services.Teleports
{
    public class TeleportService : ITeleportService
    {
        private readonly ISpiderRegistryService _spiderRegistry;
        private readonly IPoolObjects<Teleport> _teleportPools;

        private Teleport _slotA;
        private Teleport _slotB;

        public bool HasBothTeleports => _slotA != null && _slotB != null;

        public TeleportService(ISpiderRegistryService spiderRegistry, IPoolObjects<Teleport> teleportPools)
        {
            _spiderRegistry = spiderRegistry;
            _teleportPools = teleportPools;
        }

        public Teleport SpawnNewTeleport(Vector3 position, Vector3 surfaceNormal)
        {
            if (_slotA != null)
            {
                DOTween.Kill(_slotA.transform);
                _teleportPools.ReturnObjectToPool(_slotA);
            }

            _slotA = _slotB;
            _slotB = GetTeleport(position, surfaceNormal);

            return _slotB;
        }

        public void TryTeleportSpider(Teleport from)
        {
            if (!HasBothTeleports)
                return;

            Teleport target = from == _slotA ? _slotB : _slotA;
            Spider spider = _spiderRegistry.Spider;

            spider.transform.position = target.transform.position + target.transform.forward * 0.5f;
            spider.transform.rotation = target.transform.rotation;
        }

        public Teleport GetTeleport(Vector3 position, Vector3 surfaceNormal)
        {
            Teleport teleport = _teleportPools.GetObjectFromPool();
            teleport.transform.localScale = Vector3.zero;
            teleport.transform.position = position;

            teleport.transform.rotation = Quaternion.LookRotation(-surfaceNormal, Vector3.up);
            teleport.transform.DOScale(Vector3.one, 0.25f);

            return teleport;
        }
    }
}