using UnityEngine;

namespace Common
{
    public interface ICheckpointInfo
    {
        Vector3 FlowerPutdownPosition { get; }
        Quaternion FlowerPutdownRotation { get; }
    }
}