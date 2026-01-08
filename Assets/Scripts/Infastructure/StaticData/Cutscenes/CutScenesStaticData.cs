using System.Collections.Generic;
using Infastructure.CutScenes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Infastructure.StaticData.Cutscenes
{
    [CreateAssetMenu(fileName = "CutScenesData", menuName = "StaticData/CutScenesData")]
    public class CutScenesStaticData : SerializedScriptableObject
    {
        public Dictionary<CutsceneId, GameObject> Cutscenes = new Dictionary<CutsceneId, GameObject>();
    }
}