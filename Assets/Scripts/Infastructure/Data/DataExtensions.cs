using System;
using UnityEngine;

namespace Infastructure.Data
{
    public static class DataExtensions
    {
        public static string ToJson(this object obj) =>
            JsonUtility.ToJson(obj);

        public static T ToDeserialized<T>(this string json) =>
            JsonUtility.FromJson<T>(json);

        public static string LevelUp(this string levelKey)
        {
            if (string.IsNullOrEmpty(levelKey))
                return "Level_0";

            string[] levelSplit = levelKey.Split('_');
            int levelNumber = Convert.ToInt32(levelSplit[^1]);

            return $"Level_{++levelNumber}";
        }
    }
}