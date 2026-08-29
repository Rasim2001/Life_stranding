using UnityEngine;
using WeatherSystem.Profiles;

namespace WeatherSystem
{
    // Общий применятель света для солнца и луны — до среза 1 рантайм (WeatherService) и
    // Edit Mode превью (WeatherPreviewDriver) держали дословные копии тел ApplySun/ApplyMoon,
    // и геометрический порог enabled = elevation > 0f остался в обеих, хотя суточные градиенты
    // авторены под гашение по яркости (щелчок света и теней, см.
    // .scratch/celestial-handover-and-arc-controls/spec.md). Одна точка — нельзя забыть
    // перенести исправление в одну из двух реализаций. Тот же мотив, что у WeatherSkyApplier.
    //
    // Полоса фейда и параметры теней — аргументы, а не константы: с среза 2 они читаются
    // с полей WeatherRig, левел-дизайнер крутит их с панели Weather Control.
    public static class WeatherCelestialApplier
    {
        // Порог эффективной светимости (цвет × вес горизонта), ниже которого лампу выключаем —
        // не строгий ноль, чтобы не держать draw call на свете без видимого вклада.
        private const float EnabledThreshold = 0.001f;

        public static void ApplySun(Light light, SkyState sky, float elevation, float band, LightShadows shadowType) =>
            Apply(light, sky.SunColor, sky.SunIntensity, sky.SunShadowStrength, elevation, band, shadowType);

        public static void ApplyMoon(Light light, SkyState sky, float elevation, float band, LightShadows shadowType) =>
            Apply(light, sky.MoonColor, sky.MoonIntensity, sky.MoonShadowStrength, elevation, band, shadowType);

        private static void Apply(
            Light light, Color presetColor, float presetIntensity, float shadowStrengthBase, float elevation,
            float band, LightShadows shadowType)
        {
            if (light == null)
                return;

            float w = WeatherTime.HorizonWeight(elevation, band);

            // Множитель только на RGB, не на альфу — тот же приём и та же причина, что
            // в WeatherService.MultiplyRgb (Color*float иначе тянет альфу вместе с цветом).
            Color color = new Color(presetColor.r * w, presetColor.g * w, presetColor.b * w, presetColor.a);

            // Арбитраж по эффективной светимости (цвет × вес), а не по геометрии (elevation > 0f) —
            // лампа гаснет ровно там, где гаснет градиент, без щелчка теней за кадр.
            light.enabled = color.r > EnabledThreshold || color.g > EnabledThreshold || color.b > EnabledThreshold;
            if (!light.enabled)
                return;

            light.color = color;
            light.intensity = presetIntensity;
            light.shadows = shadowType;
            light.shadowStrength = shadowStrengthBase * w;
        }
    }
}
