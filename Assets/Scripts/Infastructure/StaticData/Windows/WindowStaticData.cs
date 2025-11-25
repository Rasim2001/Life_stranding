using System.Collections.Generic;
using Infastructure.Localization;
using Localization;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Infastructure.StaticData.Windows
{
    [CreateAssetMenu(fileName = "WindowsLocalization", menuName = "StaticData/WindowsLocalizationStaticData")]
    public class WindowsLocalizationStaticData : SerializedScriptableObject
    {
        public Dictionary<TextStaticId, LocalizationText> Texts = new Dictionary<TextStaticId, LocalizationText>();
    }
}