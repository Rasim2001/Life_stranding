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
        public Vector3 SpiderSpawnPosition;

        public List<Vector3> CheckPoints;
        public List<Vector3> BatteriesPoints;
    }
}