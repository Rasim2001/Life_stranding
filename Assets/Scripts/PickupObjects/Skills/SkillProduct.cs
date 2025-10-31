using UnityEngine;

namespace PickupObjects.Skills
{
    public class SkillProduct : MonoBehaviour, IProduct
    {
        public ProductType ProductType { get; set; }
    }
}