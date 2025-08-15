using HUD;
using Infastructure.StaticData;
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
                gameData.FinishTargetPosition = FindObjectOfType<TargetPointIndicatorMarker>().transform.position;

            EditorUtility.SetDirty(gameData);
        }
    }
}