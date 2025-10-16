using UnityEngine;

namespace Infastructure.Common.Pickup
{
    public interface IPickupDisplayer
    {
        void Show(Transform pickupTarget);
        void Hide(Transform pickupTarget);
    }
}