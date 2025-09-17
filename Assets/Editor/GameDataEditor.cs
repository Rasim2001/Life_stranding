using System.Linq;
using HUD;
using Infastructure.StaticData;
using PickupObjects;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(GameStaticData))]
    public class GameDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GameStaticData gameData = (GameStaticData)target;

            if (GUILayout.Button("Collect"))
            {
                gameData.CheckPoints = FindObjectsOfType<CheckPointMarker>()
                    .OrderBy(x => x.transform.GetSiblingIndex())
                    .Select(x => x.transform.position)
                    .ToList();

                gameData.BatteriesPoints = FindObjectsOfType<BatteryMarkerPoint>()
                    .OrderBy(x => x.transform.GetSiblingIndex())
                    .Select(x => x.transform.position)
                    .ToList();
            }


            EditorUtility.SetDirty(gameData);
        }
    }
}