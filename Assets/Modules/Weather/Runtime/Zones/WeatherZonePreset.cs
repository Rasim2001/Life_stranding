using UnityEngine;
using WeatherSystem.Profiles;

namespace WeatherSystem
{
    // Погодные данные зоны — отдельным компонентом, а не полем WeatherZone. Зона отвечает
    // за геометрию, вес и приоритет; что именно она приносит — дело модулей на том же объекте
    // (спек, решение 4, паттерн взят у Cozy, где биом сам данных погоды не несёт).
    //
    // Смысл разделения практический: FX — пыль, снег, дождь — станут ещё одним модулем рядом,
    // и добавить их можно будет, не трогая уже расставленные по уровню зоны и не заводя
    // второй тип зоны со своей геометрией.
    [ExecuteAlways]
    [RequireComponent(typeof(WeatherZone))]
    public class WeatherZonePreset : MonoBehaviour
    {
        [Tooltip("Состояние неба внутри объёма. Тот же тип ассета, что назначается на полосы рига.")]
        [SerializeField] private WeatherPreset _preset;

        public WeatherPreset Preset => _preset;
    }
}
