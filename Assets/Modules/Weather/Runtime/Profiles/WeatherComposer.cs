using System.Collections.Generic;
using UnityEngine;

namespace WeatherSystem.Profiles
{
    // Композиция двух осей погоды в одно состояние. Ось высоты — полосы рига, их смешивает
    // SkyBandBlender. Ось места — зоны влияния, они накладываются поверх собранного.
    //
    // Отдельный класс, а не метод блендера: блендер знает про полосы и ни про что больше,
    // а здесь встречаются обе оси. Пофайлового смешения (~40 полей) тут нет — зовём
    // SkyBandBlender.Lerp. Вторая копия этой арифметики и есть та самая видимая ступенька
    // цвета на стыке, из-за которой в спеке записано правило единственной точки правды.
    //
    // Зовут оба потребителя: WeatherService в игре и WeatherEditorDriver в редакторе.
    // Один путь — одно поведение.
    public static class WeatherComposer
    {
        // Тикет 07: предупреждаем про равный приоритет один раз на пару зон, а не каждый
        // кадр — свёртка живёт в per-frame пути, без глушения консоль забьётся за секунду.
        // Ключ — упорядоченная пара имён, чтобы (A,B) и (B,A) считались одной парой.
        private static readonly HashSet<(string, string)> WarnedPriorityPairs = new HashSet<(string, string)>();

        // observerPositionWS — null, пока камеры нет (до BuildLevelState). Тогда зоны
        // не применяются вовсе: их вес — функция расстояния до наблюдателя, а наблюдателя
        // ещё не существует. Врать нулём здесь опаснее, чем не считать: зона у начала
        // координат сработала бы на пустом месте.
        public static SkyState Compose(
            SkyBand[] bands,
            float worldY,
            float timeOfDay01,
            Vector3? observerPositionWS,
            float deltaTime)
        {
            SkyState result = SkyBandBlender.Evaluate(bands, worldY, timeOfDay01);

            if (!observerPositionWS.HasValue)
                return result;

            // Обход обязан идти по приоритету, а не по порядку регистрации — иначе итог
            // зависит от того, в каком порядке зоны включились при загрузке сцены.
            WeatherZoneRegistry.SortByPriority();
            IReadOnlyList<WeatherZone> zones = WeatherZoneRegistry.Zones;

            WeatherZone previousApplied = null;

            for (int i = 0; i < zones.Count; i++)
            {
                WeatherZone zone = zones[i];

                // Порядок вызовов Unity при перезагрузке домена и выгрузке сцены гарантировать
                // нельзя, поэтому уничтоженную зону в списке считаем нормой, а не поводом упасть.
                if (zone == null)
                    continue;

                // Вес всех зон обновляется всегда, до любых пропусков: временнóй режим
                // держит состояние между кадрами, и пропуск обновления заморозил бы его.
                float weight = zone.UpdateWeight(observerPositionWS.Value, deltaTime);
                if (weight <= 0f)
                    continue;

                WeatherPreset preset = zone.Preset;
                if (preset == null)
                    continue;

                if (previousApplied != null && previousApplied.Priority == zone.Priority)
                    WarnEqualPriority(previousApplied, zone);

                result = SkyBandBlender.Lerp(
                    result,
                    SkyBandBlender.EvaluateSingle(preset, timeOfDay01),
                    weight);

                previousApplied = zone;
            }

            return result;
        }

        private static void WarnEqualPriority(WeatherZone a, WeatherZone b)
        {
            (string, string) key = string.CompareOrdinal(a.name, b.name) <= 0
                ? (a.name, b.name)
                : (b.name, a.name);

            if (!WarnedPriorityPairs.Add(key))
                return;

            Debug.LogWarning(
                $"[WeatherComposer] Зоны '{a.name}' и '{b.name}' перекрываются с одинаковым " +
                $"приоритетом ({a.Priority}). Победила '{b.name}' (по имени) — назначь разные " +
                "приоритеты, если порядок важен.");
        }
    }
}
