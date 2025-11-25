using System;
using System.Collections.Generic;
using Localization;
using PickupObjects;
using Sirenix.OdinInspector;
using SpiderController.Platform;
using UnityEngine;
using UnityEngine.Video;

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
        [FoldoutGroup("OnPlatformSettings")] public float Weight;

        [FoldoutGroup("Description")] public ProductDescription ProductDescription;
    }

    [Serializable]
    public class ProductDescription
    {
        public LocalizationText TitleText = new();
        public LocalizationText HowToUseText = new();
        public LocalizationText DescriptionText = new();

        public VideoClip VideoClip;
    }
}