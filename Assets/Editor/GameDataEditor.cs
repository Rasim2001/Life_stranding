using System;
using System.Linq;
using Common.SceneMarkers;
using Infastructure.StaticData;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Editor
{
    [CustomEditor(typeof(GameStaticData))]
    public class GameDataEditor : OdinEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GameStaticData gameData = (GameStaticData)target;

            if (GUILayout.Button("Collect"))
            {
                string nameScene = SceneManager.GetActiveScene().name;

                if (!gameData.GameDatas.ContainsKey(nameScene))
                    gameData.GameDatas[nameScene] = new GameData();

                gameData.GameDatas[nameScene].CheckPoints = FindObjectsOfType<CheckPointMarker>()
                    .OrderBy(x => x.transform.GetSiblingIndex())
                    .Select(x => new WorldData(x.transform.position, x.transform.rotation))
                    .ToList();

                gameData.GameDatas[nameScene].BatteriesPoints = FindObjectsOfType<BatteryPointMarker>()
                    .Select(x => new WorldData(x.transform.position, x.transform.rotation))
                    .ToList();

                gameData.GameDatas[nameScene].EnergyPoints = FindObjectsOfType<EnergyPointMarker>()
                    .Select(x => new WorldData(x.transform.position, x.transform.rotation))
                    .ToList();

                gameData.GameDatas[nameScene].ElephantPoints = FindObjectsOfType<ElephantPointMarker>()
                    .Select(x => new WorldData(x.transform.position, x.transform.rotation))
                    .ToList();

                SpiderSpawnPointMarker spiderSpawnPointMarker = FindObjectOfType<SpiderSpawnPointMarker>();

                gameData.GameDatas[nameScene].SpiderSpawnData = new WorldData(
                    spiderSpawnPointMarker.transform.position,
                    spiderSpawnPointMarker.transform.rotation);
            }


            EditorUtility.SetDirty(gameData);
        }
    }
}