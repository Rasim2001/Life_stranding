using System.Collections.Generic;
using PickupObjects;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Infastructure.StaticData.XRay
{
    [CreateAssetMenu(fileName = "XRay", menuName = "StaticData/XRayCollection")]
    public class XRayCollectionStaticData : SerializedScriptableObject
    {
        public Dictionary<ProductType, Sprite> XRayObjects = new Dictionary<ProductType, Sprite>();
    }
}