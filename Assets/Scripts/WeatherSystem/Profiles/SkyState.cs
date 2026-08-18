using UnityEngine;

namespace WeatherSystem.Profiles
{
    // Финальные значения для шейдера неба — единственная точка, которую читает
    // WeatherService.ApplySky(). Откуда они посчитаны (какая полоса, какое время суток),
    // читателю этой структуры безразлично.
    public struct SkyState
    {
        public Color ZenithColor;
        public Color HorizonColor;
        public float GradientExponent;

        public Color CloudColor;
        public Color CloudShadowColor;
        public Color CloudHighlightColor;
        public float CloudCoverage;
        public float CloudScale;
        public float CloudSoftness;
        public float WindSpeed;
        public float CloudRollBias;
        public float CloudHighlightFalloff;
        public float CloudDetailScale;
        public float CloudDetailAmount;
        public float ShadowSampleDistance;
        public float ShadowDensity;
        public float CloudThickness;
        public float BorderEffect;
        public float BorderHeight;

        public Color StormColor;
        public Color StormShadowColor;
        public float StormScale;
        public float StormThreshold;
        public Vector3 StormDirection;
        public float StormFrontFalloff;

        public Color CirrusColor;
        public float CirrusCoverage;
        public float CirrusOpacity;
        public float CirrusScale;
        public float CirrusSpeed;

        public Color AmbientSkyColor;
        public Color AmbientEquatorColor;
        public Color AmbientGroundColor;
        public float AmbientMultiplier;

        public float SunIntensity;
        public Color SunColor;
        public float SunSize;
        public Color SunHaloColor;
        public float SunHaloFalloff;
        public float SunHaloIntensity;

        public float MoonIntensity;
        public Color MoonColor;
        public float MoonFlareFalloff;
        public float MoonFlareIntensity;

        public float StarDensity;
        public Color NightTint;

        public Color SkyFogColor;
        public float SkyFogAmount;
        public float SkyFogHeight;
        public float SkyFogGlowSquish;

        public Color FilterColor;
        public float FilterSaturation;
        public float FilterValue;
    }
}
