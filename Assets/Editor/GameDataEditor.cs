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
                    .Select(x => x.transform.position)
                    .ToList();

                gameData.GameDatas[nameScene].BatteriesPoints = FindObjectsOfType<BatteryPointMarker>()
                    .OrderBy(x => x.transform.GetSiblingIndex())
                    .Select(x => x.transform.position)
                    .ToList();

                gameData.GameDatas[nameScene].SpiderSpawnPosition =
                    FindObjectOfType<SpiderSpawnPointMarker>().transform.position;
            }


            EditorUtility.SetDirty(gameData);
        }
    }
}