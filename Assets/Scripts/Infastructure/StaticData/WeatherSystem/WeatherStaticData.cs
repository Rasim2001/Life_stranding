using UnityEngine;
using WeatherSystem.Profiles;

namespace Infastructure.StaticData.WeatherSystem
{
    // Реестр высотных полос. Само состояние атмосферы (небо, облака, ambient, солнце,
    // луна, звёзды) живёт в SkyBandProfile каждой полосы — сюда оно не переезжает
    // обратно, чтобы не повторить судьбу одного общего мешка настроек на всех.
    [CreateAssetMenu(fileName = "WeatherData", menuName = "StaticData/WeatherData")]
    public class WeatherStaticData : ScriptableObject
    {
        // Отсортированы по возрастанию StartY — см. контракт в SkyBand.cs.
        [Header("Sky bands by world height")]
        public SkyBand[] Bands;
    }
}
