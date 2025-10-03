using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Infastructure.StaticData.XRay
{
    [CreateAssetMenu(fileName = "XRay", menuName = "StaticData/XRayCollection")]
    public class XRayCollectionStaticData : SerializedScriptableObject
    {
        public Dictionary<XRayType, Sprite> XRayObjects = new Dictionary<XRayType, Sprite>();
    }
}