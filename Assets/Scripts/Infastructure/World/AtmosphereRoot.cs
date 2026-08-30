using UnityEngine;
using UnityEngine.Rendering;
using WeatherSystem;

namespace Infastructure.World
{
    // Контракт сцены атмосферы: единственная точка, через которую игровой слой достаёт
    // объекты, физически живущие в другой сцене. Межсценовых сериализованных ссылок
    // в Unity нет — см. .scratch/plans/shiny-bouncing-lerdorf.md.
    public class AtmosphereRoot : MonoBehaviour
    {
        [SerializeField] private Volume _globalVolume;
        [SerializeField] private WeatherRig _weatherRig;

        public Volume GlobalVolume => _globalVolume;
        public WeatherRig WeatherRig => _weatherRig;
    }
}
