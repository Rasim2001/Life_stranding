using UnityEngine;

namespace WeatherSystem.Profiles
{
    // Резолв пары соседних высотных полос по мировой высоте + суточное значение внутри
    // каждой. Plain C#, не MonoBehaviour/ScriptableObject — вызывается из WeatherService.Tick().
    public static class SkyBandBlender
    {
        // Последовательность зон по возрастанию Y: плато(0) -> переход(0->1) -> плато(1)
        // -> переход(1->2) -> ... Переход определяется ТОЛЬКО текущей полосой
        // (EndY + BlendUpwards), следующая полоса своим StartY/EndY на его ширину
        // не влияет — так переходы A->B и B->C настраиваются независимо, как и просили.
        public static SkyState Evaluate(SkyBand[] bands, float worldY, float timeOfDay01)
        {
            if (bands == null || bands.Length == 0)
                return default;

            if (bands.Length == 1)
                return EvaluateSingle(bands[0].Profile, timeOfDay01);

            if (worldY <= bands[0].EndY)
                return EvaluateSingle(bands[0].Profile, timeOfDay01);

            for (int i = 0; i < bands.Length - 1; i++)
            {
                SkyBand current = bands[i];
                SkyBand next = bands[i + 1];
                float blendEnd = current.EndY + current.BlendUpwards;

                if (worldY <= blendEnd)
                {
                    float t = current.BlendUpwards > 0f
                        ? SmoothStep01(Mathf.InverseLerp(current.EndY, blendEnd, worldY))
                        : 1f;
                    return Lerp(
                        EvaluateSingle(current.Profile, timeOfDay01),
                        EvaluateSingle(next.Profile, timeOfDay01),
                        t);
                }

                if (worldY <= next.EndY)
                    return EvaluateSingle(next.Profile, timeOfDay01);
            }

            return EvaluateSingle(bands[bands.Length - 1].Profile, timeOfDay01);
        }

        private static float SmoothStep01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        private static SkyState EvaluateSingle(SkyBandProfile profile, float timeOfDay01)
        {
            if (profile == null)
                return default;

            return new SkyState
            {
                ZenithColor = profile.SkyZenithColor.Evaluate(timeOfDay01),
                HorizonColor = profile.SkyHorizonColor.Evaluate(timeOfDay01),
                GradientExponent = profile.GradientExponent.Evaluate(timeOfDay01),

                CloudColor = profile.CloudColor.Evaluate(timeOfDay01),
                CloudShadowColor = profile.CloudShadowColor.Evaluate(timeOfDay01),
                CloudHighlightColor = profile.CloudHighlightColor.Evaluate(timeOfDay01),
                CloudCoverage = profile.CloudCoverage.Evaluate(timeOfDay01),
                CloudScale = profile.CloudScale,
                CloudSoftness = profile.CloudSoftness,
                WindSpeed = profile.WindSpeed,
                CloudRollBias = profile.CloudRollBias,
                CloudHighlightFalloff = profile.CloudHighlightFalloff,
                CloudDetailScale = profile.CloudDetailScale,
                CloudDetailAmount = profile.CloudDetailAmount,
                ShadowSampleDistance = profile.ShadowSampleDistance,
                ShadowDensity = profile.ShadowDensity,
                CloudThickness = profile.CloudThickness,
                BorderEffect = profile.BorderEffect,
                BorderHeight = profile.BorderHeight,

                StormColor = profile.StormColor.Evaluate(timeOfDay01),
                StormShadowColor = profile.StormShadowColor.Evaluate(timeOfDay01),
                StormScale = profile.StormScale,
                StormThreshold = profile.StormThreshold,
                StormDirection = profile.StormDirection,
                StormFrontFalloff = profile.StormFrontFalloff,

                CirrusColor = profile.CirrusColor.Evaluate(timeOfDay01),
                CirrusCoverage = profile.CirrusCoverage.Evaluate(timeOfDay01),
                CirrusOpacity = profile.CirrusOpacity.Evaluate(timeOfDay01),
                CirrusScale = profile.CirrusScale,
                CirrusSpeed = profile.CirrusSpeed,

                AmbientSkyColor = profile.AmbientSkyColor.Evaluate(timeOfDay01),
                AmbientEquatorColor = profile.AmbientEquatorColor.Evaluate(timeOfDay01),
                AmbientGroundColor = profile.AmbientGroundColor.Evaluate(timeOfDay01),
                AmbientMultiplier = profile.AmbientMultiplier.Evaluate(timeOfDay01),

                SunIntensity = profile.SunIntensity.Evaluate(timeOfDay01),
                SunColor = profile.SunColor.Evaluate(timeOfDay01),
                SunSize = profile.SunSize,
                SunHaloColor = profile.SunHaloColor.Evaluate(timeOfDay01),
                SunHaloFalloff = profile.SunHaloFalloff,
                SunHaloIntensity = profile.SunHaloIntensity.Evaluate(timeOfDay01),

                MoonIntensity = profile.MoonIntensity.Evaluate(timeOfDay01),
                MoonColor = profile.MoonColor.Evaluate(timeOfDay01),
                MoonFlareFalloff = profile.MoonFlareFalloff,
                MoonFlareIntensity = profile.MoonFlareIntensity.Evaluate(timeOfDay01),

                StarDensity = profile.StarDensity,
                NightTint = profile.NightTint,

                SkyFogColor = profile.SkyFogColor.Evaluate(timeOfDay01),
                SkyFogAmount = profile.SkyFogAmount.Evaluate(timeOfDay01),
                SkyFogHeight = profile.SkyFogHeight,
                SkyFogGlowSquish = profile.SkyFogGlowSquish,

                FilterColor = profile.FilterColor,
                FilterSaturation = profile.FilterSaturation,
                FilterValue = profile.FilterValue
            };
        }

        private static SkyState Lerp(SkyState a, SkyState b, float t) => new SkyState
        {
            ZenithColor = Color.Lerp(a.ZenithColor, b.ZenithColor, t),
            HorizonColor = Color.Lerp(a.HorizonColor, b.HorizonColor, t),
            GradientExponent = Mathf.Lerp(a.GradientExponent, b.GradientExponent, t),

            CloudColor = Color.Lerp(a.CloudColor, b.CloudColor, t),
            CloudShadowColor = Color.Lerp(a.CloudShadowColor, b.CloudShadowColor, t),
            CloudHighlightColor = Color.Lerp(a.CloudHighlightColor, b.CloudHighlightColor, t),
            CloudCoverage = Mathf.Lerp(a.CloudCoverage, b.CloudCoverage, t),
            CloudScale = Mathf.Lerp(a.CloudScale, b.CloudScale, t),
            CloudSoftness = Mathf.Lerp(a.CloudSoftness, b.CloudSoftness, t),
            WindSpeed = Mathf.Lerp(a.WindSpeed, b.WindSpeed, t),
            CloudRollBias = Mathf.Lerp(a.CloudRollBias, b.CloudRollBias, t),
            CloudHighlightFalloff = Mathf.Lerp(a.CloudHighlightFalloff, b.CloudHighlightFalloff, t),
            CloudDetailScale = Mathf.Lerp(a.CloudDetailScale, b.CloudDetailScale, t),
            CloudDetailAmount = Mathf.Lerp(a.CloudDetailAmount, b.CloudDetailAmount, t),
            ShadowSampleDistance = Mathf.Lerp(a.ShadowSampleDistance, b.ShadowSampleDistance, t),
            ShadowDensity = Mathf.Lerp(a.ShadowDensity, b.ShadowDensity, t),
            CloudThickness = Mathf.Lerp(a.CloudThickness, b.CloudThickness, t),
            BorderEffect = Mathf.Lerp(a.BorderEffect, b.BorderEffect, t),
            BorderHeight = Mathf.Lerp(a.BorderHeight, b.BorderHeight, t),

            StormColor = Color.Lerp(a.StormColor, b.StormColor, t),
            StormShadowColor = Color.Lerp(a.StormShadowColor, b.StormShadowColor, t),
            StormScale = Mathf.Lerp(a.StormScale, b.StormScale, t),
            StormThreshold = Mathf.Lerp(a.StormThreshold, b.StormThreshold, t),
            StormDirection = Vector3.Lerp(a.StormDirection, b.StormDirection, t),
            StormFrontFalloff = Mathf.Lerp(a.StormFrontFalloff, b.StormFrontFalloff, t),

            CirrusColor = Color.Lerp(a.CirrusColor, b.CirrusColor, t),
            CirrusCoverage = Mathf.Lerp(a.CirrusCoverage, b.CirrusCoverage, t),
            CirrusOpacity = Mathf.Lerp(a.CirrusOpacity, b.CirrusOpacity, t),
            CirrusScale = Mathf.Lerp(a.CirrusScale, b.CirrusScale, t),
            CirrusSpeed = Mathf.Lerp(a.CirrusSpeed, b.CirrusSpeed, t),

            AmbientSkyColor = Color.Lerp(a.AmbientSkyColor, b.AmbientSkyColor, t),
            AmbientEquatorColor = Color.Lerp(a.AmbientEquatorColor, b.AmbientEquatorColor, t),
            AmbientGroundColor = Color.Lerp(a.AmbientGroundColor, b.AmbientGroundColor, t),
            AmbientMultiplier = Mathf.Lerp(a.AmbientMultiplier, b.AmbientMultiplier, t),

            SunIntensity = Mathf.Lerp(a.SunIntensity, b.SunIntensity, t),
            SunColor = Color.Lerp(a.SunColor, b.SunColor, t),
            SunSize = Mathf.Lerp(a.SunSize, b.SunSize, t),
            SunHaloColor = Color.Lerp(a.SunHaloColor, b.SunHaloColor, t),
            SunHaloFalloff = Mathf.Lerp(a.SunHaloFalloff, b.SunHaloFalloff, t),
            SunHaloIntensity = Mathf.Lerp(a.SunHaloIntensity, b.SunHaloIntensity, t),

            MoonIntensity = Mathf.Lerp(a.MoonIntensity, b.MoonIntensity, t),
            MoonColor = Color.Lerp(a.MoonColor, b.MoonColor, t),
            MoonFlareFalloff = Mathf.Lerp(a.MoonFlareFalloff, b.MoonFlareFalloff, t),
            MoonFlareIntensity = Mathf.Lerp(a.MoonFlareIntensity, b.MoonFlareIntensity, t),

            StarDensity = Mathf.Lerp(a.StarDensity, b.StarDensity, t),
            NightTint = Color.Lerp(a.NightTint, b.NightTint, t),

            SkyFogColor = Color.Lerp(a.SkyFogColor, b.SkyFogColor, t),
            SkyFogAmount = Mathf.Lerp(a.SkyFogAmount, b.SkyFogAmount, t),
            SkyFogHeight = Mathf.Lerp(a.SkyFogHeight, b.SkyFogHeight, t),
            SkyFogGlowSquish = Mathf.Lerp(a.SkyFogGlowSquish, b.SkyFogGlowSquish, t),

            FilterColor = Color.Lerp(a.FilterColor, b.FilterColor, t),
            FilterSaturation = Mathf.Lerp(a.FilterSaturation, b.FilterSaturation, t),
            FilterValue = Mathf.Lerp(a.FilterValue, b.FilterValue, t)
        };
    }
}
