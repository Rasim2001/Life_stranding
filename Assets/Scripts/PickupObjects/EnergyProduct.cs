using UnityEngine;

namespace PickupObjects
{
    public class EnergyProduct : MonoBehaviour, IProduct
    {
        [field: SerializeField] public ProductType ProductType { get; set; }
    }
}