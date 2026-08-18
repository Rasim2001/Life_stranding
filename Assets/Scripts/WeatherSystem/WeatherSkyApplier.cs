using UnityEngine;
using WeatherSystem.Profiles;

namespace WeatherSystem
{
    // Приёмник свойств шейдера неба/облаков — по нему пишет и рантайм (в Material,
    // см. MaterialSink), и Edit Mode превью (в MaterialPropertyBlock, см.
    // WeatherPreviewDriver в Assets/Editor/Weather). Один интерфейс — одна точка,
    // где нельзя забыть добавленное свойство в одну из двух реализаций.
    public interface IWeatherPropertySink
    {
        void SetColor(int id, Color value);
        void SetFloat(int id, float value);
        void SetVector(int id, Vector4 value);
    }

    // Обёртка над Material — то, чем сейчас пишет WeatherService.Tick() каждый кадр.
    public readonly struct MaterialSink : IWeatherPropertySink
    {
        private readonly Material _material;

        public MaterialSink(Material material) => _material = material;

        public void SetColor(int id, Color value) => _material.SetColor(id, value);
        public void SetFloat(int id, float value) => _material.SetFloat(id, value);
        public void SetVector(int id, Vector4 value) => _material.SetVector(id, value);
    }

    // Единственное место, где перечислены свойства шейдеров SHD_Weather_DomeSky /
    // SHD_Weather_DomeClouds (было раньше WeatherService.ApplySkyToMaterial).
    // Вынесено сюда, чтобы и рантайм, и Edit Mode превью шли по одному списку:
    // добавят свойство в шейдер и заведут его в рантайме — превью не должно суметь
    // "забыть" то же самое и начать молча врать.
    //
    // TSink — дженерик со struct-ограничением, а не параметр типа IWeatherPropertySink:
    // на ~50 вызовов за кадр упаковка интерфейса в ссылочный тип была бы лишней
    // аллокацией каждый Tick; JIT специализирует generic под конкретный struct
    // и зовёт методы напрямую, без диспетчеризации через интерфейс.
    public static class WeatherSkyApplier
    {
        public static void Apply<TSink>(TSink sink, SkyState sky, float nightFactor)
            where TSink : struct, IWeatherPropertySink
        {
            sink.SetColor(WeatherShaderIds.ZenithColor, sky.ZenithColor);
            sink.SetColor(WeatherShaderIds.HorizonColor, sky.HorizonColor);
            sink.SetFloat(WeatherShaderIds.GradientExponent, sky.GradientExponent);
            sink.SetFloat(WeatherShaderIds.NightFactor, nightFactor);
            sink.SetColor(WeatherShaderIds.NightTint, sky.NightTint);
            sink.SetFloat(WeatherShaderIds.StarDensity, sky.StarDensity);

            sink.SetColor(WeatherShaderIds.CloudColor, sky.CloudColor);
            sink.SetColor(WeatherShaderIds.CloudShadowColor, sky.CloudShadowColor);
            sink.SetColor(WeatherShaderIds.CloudHighlightColor, sky.CloudHighlightColor);
            sink.SetFloat(WeatherShaderIds.CloudCoverage, sky.CloudCoverage);
            sink.SetFloat(WeatherShaderIds.CloudScale, sky.CloudScale);
            sink.SetFloat(WeatherShaderIds.CloudSoftness, sky.CloudSoftness);
            sink.SetFloat(WeatherShaderIds.WindSpeed, sky.WindSpeed);
            sink.SetFloat(WeatherShaderIds.CloudRollBias, sky.CloudRollBias);
            sink.SetFloat(WeatherShaderIds.CloudHighlightFalloff, sky.CloudHighlightFalloff);
            sink.SetFloat(WeatherShaderIds.CloudDetailScale, sky.CloudDetailScale);
            sink.SetFloat(WeatherShaderIds.CloudDetailAmount, sky.CloudDetailAmount);
            sink.SetFloat(WeatherShaderIds.ShadowSampleDistance, sky.ShadowSampleDistance);
            sink.SetFloat(WeatherShaderIds.ShadowDensity, sky.ShadowDensity);
            sink.SetFloat(WeatherShaderIds.CloudThickness, sky.CloudThickness);
            sink.SetFloat(WeatherShaderIds.BorderEffect, sky.BorderEffect);
            sink.SetFloat(WeatherShaderIds.BorderHeight, sky.BorderHeight);

            sink.SetColor(WeatherShaderIds.StormColor, sky.StormColor);
            sink.SetColor(WeatherShaderIds.StormShadowColor, sky.StormShadowColor);
            sink.SetFloat(WeatherShaderIds.StormScale, sky.StormScale);
            sink.SetFloat(WeatherShaderIds.StormThreshold, sky.StormThreshold);
            sink.SetVector(WeatherShaderIds.StormDirection, sky.StormDirection);
            sink.SetFloat(WeatherShaderIds.StormFrontFalloff, sky.StormFrontFalloff);

            sink.SetColor(WeatherShaderIds.CirrusColor, sky.CirrusColor);
            sink.SetFloat(WeatherShaderIds.CirrusCoverage, sky.CirrusCoverage);
            sink.SetFloat(WeatherShaderIds.CirrusOpacity, sky.CirrusOpacity);
            sink.SetFloat(WeatherShaderIds.CirrusScale, sky.CirrusScale);
            sink.SetFloat(WeatherShaderIds.CirrusSpeed, sky.CirrusSpeed);

            // Двойное назначение: тот же цвет уже красит Light-источник в ApplySun/ApplyMoon.
            sink.SetColor(WeatherShaderIds.SunColor, sky.SunColor);
            sink.SetFloat(WeatherShaderIds.SunSize, sky.SunSize);
            sink.SetColor(WeatherShaderIds.SunHaloColor, sky.SunHaloColor);
            sink.SetFloat(WeatherShaderIds.SunHaloFalloff, sky.SunHaloFalloff);
            sink.SetFloat(WeatherShaderIds.SunHaloIntensity, sky.SunHaloIntensity);

            sink.SetColor(WeatherShaderIds.MoonFlareColor, sky.MoonColor);
            sink.SetFloat(WeatherShaderIds.MoonFlareFalloff, sky.MoonFlareFalloff);
            sink.SetFloat(WeatherShaderIds.MoonFlareIntensity, sky.MoonFlareIntensity);

            sink.SetColor(WeatherShaderIds.SkyFogColor, sky.SkyFogColor);
            sink.SetFloat(WeatherShaderIds.SkyFogAmount, sky.SkyFogAmount);
            sink.SetFloat(WeatherShaderIds.SkyFogHeight, sky.SkyFogHeight);
            sink.SetFloat(WeatherShaderIds.SkyFogGlowSquish, sky.SkyFogGlowSquish);

            sink.SetColor(WeatherShaderIds.FilterColor, sky.FilterColor);
            sink.SetFloat(WeatherShaderIds.FilterSaturation, sky.FilterSaturation);
            sink.SetFloat(WeatherShaderIds.FilterValue, sky.FilterValue);
        }
    }
}
