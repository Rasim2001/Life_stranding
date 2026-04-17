using PickupObjects.Teleports;
using UnityEngine;

namespace Infastructure.Services.Teleports
{
    public interface ITeleportService
    {
        Teleport SpawnNewTeleport(Vector3 position, Vector3 surfaceNormal);
        void TryTeleportSpider(Teleport from);
        Teleport GetTeleport(Vector3 position, Vector3 surfaceNormal);
    }
}