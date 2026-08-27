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
        public Color CloudSkyLitColor;
        public Color CloudShadowColor;
        public Color CloudHighlightColor;
        public Color CloudMoonColor;
        public float CloudCoverage;
        public float CloudScale;
        public float CloudSoftness;
        public float WindSpeed;
        public float CloudRollBias;
        public float CloudHighlightFalloff;
        public float CloudMoonHighlightFalloff;
        public float CloudDetailScale;
        public float CloudDetailAmount;
        public float CloudCohesion;
        public float ShadowSampleDistance;
        public float ShadowDensity;
        public float CloudThickness;
        public float BorderEffect;
        public float BorderHeight;
        public Color CloudBorderColor;
        public float SkyLitSpread;
        public float SkyLitSoftness;

        public Color StormTint;
        public float StormCoverage;
        public float StormScale;
        public float StormThreshold;
        public Vector3 StormDirection;
        public float StormFrontFalloff;

        public Color CirrusTint;
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

        public Color StarColor;
        public float Latitude;

        public float SkyFogAmount;
        public float SkyFogHeight;
        public float SkyFogGlowSquish;

        // Туман на мировой геометрии — рампа и плотность свои, цвет дальнего стопа общий
        // с дымкой купола (SkyFogAmount/Height/GlowSquish выше) и с подмесом в облака,
        // см. SkyBandProfile.
        public Color FogNearColor;
        public Color FogMidColor;
        public Color FogFarColor;
        public float FogMidPosition;
        public float FogFarPosition;
        public float FogVisibilityDistance;
        public float CloudsFogAmount;

        public Color FilterColor;
        public float FilterSaturation;
        public float FilterValue;
    }
}
