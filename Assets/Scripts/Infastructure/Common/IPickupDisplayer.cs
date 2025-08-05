using UnityEngine;

namespace Infastructure.Common
{
    public interface IPickupDisplayer
    {
        void Show(Transform pickupTarget);
        void Hide();
    }
}