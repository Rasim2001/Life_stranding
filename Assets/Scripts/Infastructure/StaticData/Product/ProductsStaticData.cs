using System.Collections.Generic;
using PickupObjects;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Infastructure.StaticData.Product
{
    [CreateAssetMenu(fileName = "ProductsData", menuName = "StaticData/ProductsData")]
    public class ProductsStaticData : SerializedScriptableObject
    {
        public Dictionary<ProductType, GameObject> ProductsDictionary;
    }
}