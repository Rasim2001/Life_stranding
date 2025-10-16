using System.Collections.Generic;
using Sirenix.OdinInspector;
using SpiderController.UI.Stickers;
using UnityEngine;

namespace Infastructure.StaticData.Stikers
{
    [CreateAssetMenu(fileName = "StickersData", menuName = "StaticData/StickersData")]
    public class StickersStaticData : SerializedScriptableObject
    {
        public Dictionary<StickerEnum, ParticleSystem> Stickers = new Dictionary<StickerEnum, ParticleSystem>();
    }
}