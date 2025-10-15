using UnityEngine;

namespace PickupObjects
{
    public class EnergyProduct : MonoBehaviour, IProduct
    {
        public ProductType ProductType { get; set; }
    }
}