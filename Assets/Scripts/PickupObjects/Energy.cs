using UnityEngine;

namespace PickupObjects
{
    public class Energy : MonoBehaviour, IProduct
    {
        public ProductType ProductType { get; set; }
    }
}