using System;
using System.Collections.Generic;
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
}