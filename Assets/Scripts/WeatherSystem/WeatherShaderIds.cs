using UnityEngine;

namespace WeatherSystem
{
    // Свойства двух купольных материалов (SHD_Weather_DomeSky, SHD_Weather_DomeClouds).
    // Оба материала получают из WeatherService общий набор свойств с одинаковыми
    // именами там, где они пересекаются (Filter*) — Material.SetFloat на несуществующем
    // в данном шейдере свойстве не ошибка, просто no-op, поэтому один и тот же вызов
    // ApplySky безопасно бьёт по обоим материалам без разбора "это для Sky или Clouds".
    public static class WeatherShaderIds
    {
        public static readonly int ZenithColor = Shader.PropertyToID("_ZenithColor");
        public static readonly int HorizonColor = Shader.PropertyToID("_HorizonColor");
        public static readonly int GradientExponent = Shader.PropertyToID("_GradientExponent");
        public static readonly int NightFactor = Shader.PropertyToID("_NightFactor");
        public static readonly int NightTint = Shader.PropertyToID("_NightTint");
        public static readonly int StarDensity = Shader.PropertyToID("_StarDensity");
        public static readonly int Alpha = Shader.PropertyToID("_Alpha");

        public static readonly int CloudColor = Shader.PropertyToID("_CloudColor");
        public static readonly int CloudShadowColor = Shader.PropertyToID("_CloudShadowColor");
        public static readonly int CloudHighlightColor = Shader.PropertyToID("_CloudHighlightColor");
        public static readonly int CloudCoverage = Shader.PropertyToID("_CloudCoverage");
        public static readonly int CloudScale = Shader.PropertyToID("_CloudScale");
        public static readonly int CloudSoftness = Shader.PropertyToID("_CloudSoftness");
        public static readonly int WindSpeed = Shader.PropertyToID("_WindSpeed");
        public static readonly int CloudRollBias = Shader.PropertyToID("_CloudRollBias");
        public static readonly int CloudHighlightFalloff = Shader.PropertyToID("_CloudHighlightFalloff");
        public static readonly int CloudDetailScale = Shader.PropertyToID("_CloudDetailScale");
        public static readonly int CloudDetailAmount = Shader.PropertyToID("_CloudDetailAmount");
        public static readonly int ShadowSampleDistance = Shader.PropertyToID("_ShadowSampleDistance");
        public static readonly int ShadowDensity = Shader.PropertyToID("_ShadowDensity");
        public static readonly int CloudThickness = Shader.PropertyToID("_CloudThickness");
        public static readonly int BorderEffect = Shader.PropertyToID("_BorderEffect");
        public static readonly int BorderHeight = Shader.PropertyToID("_BorderHeight");

        public static readonly int StormColor = Shader.PropertyToID("_StormColor");
        public static readonly int StormShadowColor = Shader.PropertyToID("_StormShadowColor");
        public static readonly int StormScale = Shader.PropertyToID("_StormScale");
        public static readonly int StormThreshold = Shader.PropertyToID("_StormThreshold");
        public static readonly int StormDirection = Shader.PropertyToID("_StormDirection");
        public static readonly int StormFrontFalloff = Shader.PropertyToID("_StormFrontFalloff");

        public static readonly int CirrusColor = Shader.PropertyToID("_CirrusColor");
        public static readonly int CirrusCoverage = Shader.PropertyToID("_CirrusCoverage");
        public static readonly int CirrusOpacity = Shader.PropertyToID("_CirrusOpacity");
        public static readonly int CirrusScale = Shader.PropertyToID("_CirrusScale");
        public static readonly int CirrusSpeed = Shader.PropertyToID("_CirrusSpeed");

        public static readonly int SunColor = Shader.PropertyToID("_SunColor");
        public static readonly int SunSize = Shader.PropertyToID("_SunSize");
        public static readonly int SunHaloColor = Shader.PropertyToID("_SunHaloColor");
        public static readonly int SunHaloFalloff = Shader.PropertyToID("_SunHaloFalloff");
        public static readonly int SunHaloIntensity = Shader.PropertyToID("_SunHaloIntensity");

        public static readonly int MoonFlareColor = Shader.PropertyToID("_MoonFlareColor");
        public static readonly int MoonFlareFalloff = Shader.PropertyToID("_MoonFlareFalloff");
        public static readonly int MoonFlareIntensity = Shader.PropertyToID("_MoonFlareIntensity");

        // Диск луны (SHD_Weather_Moon) — отдельный материал, не купол неба.
        public static readonly int MoonColor = Shader.PropertyToID("_MoonColor");
        public static readonly int MoonPhaseSharpness = Shader.PropertyToID("_PhaseSharpness");
        public static readonly int MoonPhaseOffset = Shader.PropertyToID("_PhaseOffset");

        public static readonly int SkyFogColor = Shader.PropertyToID("_SkyFogColor");
        public static readonly int SkyFogAmount = Shader.PropertyToID("_SkyFogAmount");
        public static readonly int SkyFogHeight = Shader.PropertyToID("_SkyFogHeight");
        public static readonly int SkyFogGlowSquish = Shader.PropertyToID("_SkyFogGlowSquish");

        public static readonly int FilterColor = Shader.PropertyToID("_FilterColor");
        public static readonly int FilterSaturation = Shader.PropertyToID("_FilterSaturation");
        public static readonly int FilterValue = Shader.PropertyToID("_FilterValue");

        // Глобалы (Shader.SetGlobalVector), не свойства материала — направления на светила
        // нужны не только небу, но и облакам, позже туману и воде.
        public static readonly int SunDirectionGlobal = Shader.PropertyToID("_SR_SunDirection");
        public static readonly int MoonDirectionGlobal = Shader.PropertyToID("_SR_MoonDirection");
    }
}
