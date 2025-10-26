using System;
using System.Collections.Generic;
using PickupObjects;
using Sirenix.OdinInspector;
using SpiderController.Platform;
using UnityEngine;

namespace Infastructure.StaticData.Product
{
    [CreateAssetMenu(fileName = "ProductsData", menuName = "StaticData/ProductsData")]
    public class ProductsStaticData : SerializedScriptableObject
    {
        public Dictionary<ProductType, ProductData> ProductsDictionary;
    }

    [Serializable]
    public class ProductData
    {
        public GameObject Prefab;

        [FoldoutGroup("OnPlatformSettings")] public float Speed;
        [FoldoutGroup("OnPlatformSettings")] public Vector3 StartRotationEuler;
        [FoldoutGroup("OnPlatformSettings")] public Vector3 StartPositionVector;
        [FoldoutGroup("OnPlatformSettings")] public PlatformId PlatformId;

        [FoldoutGroup("Description")] public ProductDescription ProductDescription;
    }

    [Serializable]
    public class ProductDescription
    {
        public string TitleText;
        public string HowToUseText;
        public string DescriptionText;
    }
}