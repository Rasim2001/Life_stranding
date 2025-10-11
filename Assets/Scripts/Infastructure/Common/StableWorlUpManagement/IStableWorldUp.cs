using UnityEngine;

namespace Infastructure.Common.StableWorlUpManagement
{
    public interface IStableWorldUp
    {
        void Rotate(Quaternion targetRotation);
    }
}