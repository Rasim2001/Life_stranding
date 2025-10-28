using System;
using System.Collections.Generic;
using PickupObjects;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Infastructure.StaticData
{
    [CreateAssetMenu(fileName = "GameData", menuName = "StaticData/GameData")]
    public class GameStaticData : SerializedScriptableObject
    {
        public string LoadScene;

        public Dictionary<string, GameData> GameDatas = new Dictionary<string, GameData>();
    }

    [Serializable]
    public class GameData
    {
        public WorldData SpiderSpawnData;
        public WorldData FlowerSpawnData;

        public List<WorldData> CheckPoints;
        public List<WorldData> BatteriesPoints;
        public List<WorldData> EnergyPoints;
        public List<WorldData> ElephantPoints;

        public List<ProductSkillData> SkillsData;
    }


    [Serializable]
    public class WorldData
    {
        public Vector3 WorldPosition;
        public Quaternion WorldRotation;

        public WorldData(Vector3 worldPosition, Quaternion worldRotation)
        {
            WorldPosition = worldPosition;
            WorldRotation = worldRotation;
        }
    }

    [Serializable]
    public class ProductSkillData : WorldData
    {
        public ProductType ProductType;

        public ProductSkillData(Vector3 worldPosition, Quaternion worldRotation, ProductType productType) : base(
            worldPosition, worldRotation)
        {
            ProductType = productType;
        }
    }
}