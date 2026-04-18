using PickupObjects.Teleports;
using UnityEngine;

namespace Infastructure.Services.Teleports
{
    public interface ITeleportService
    {
        Teleport SpawnNewTeleport(Vector3 position);
        void TryTeleportSpider(Teleport from);
    }
}