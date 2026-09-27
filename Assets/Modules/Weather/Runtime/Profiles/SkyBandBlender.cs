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
                return EvaluateSingle(bands[0].Preset, timeOfDay01);

            if (worldY <= bands[0].EndY)
                return EvaluateSingle(bands[0].Preset, timeOfDay01);

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
                        EvaluateSingle(current.Preset, timeOfDay01),
                        EvaluateSingle(next.Preset, timeOfDay01),
                        t);
                }

                if (worldY <= next.EndY)
                    return EvaluateSingle(next.Preset, timeOfDay01);
            }

            return EvaluateSingle(bands[bands.Length - 1].Preset, timeOfDay01);
        }

        private static float SmoothStep01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        // Ноль в поле дальности видимости означает "туман не задан", а не "видимость ноль".
        // Ноль приезжает штатными путями: новый профиль (DailyFloat._constant без
        // инициализатора), полоса с незаполненным Preset (EvaluateSingle отдаёт default),
        // DailyFloat в Curve-режиме с дефолтной нулевой кривой. Трактуем как "тумана нет" —
        // σ = 3/V при V→0 даёт сплошную пелену на весь мир, худший из дефолтов для
        // незаполненных данных. NaN сюда тоже попадает: NaN > 0 ложно.
        public const float NoFogVisibility = 1e6f;

        public static float SafeVisibility(float visibility) =>
            visibility > 0f ? visibility : NoFogVisibility;

        // V(t) = V_a · (V_b/V_a)^t — спек, решение #5.
        // Оба входа прогоняются через SafeVisibility: без этого a=0 даёт 0·Pow(∞,t) = NaN,
        // который дальше уезжает в Shader.SetGlobalFloat и красит весь кадр. [DailyRange]
        // от этого не защищает — он ограничивает только слайдер в Constant-режиме,
        // а не хранимое значение.
        private static float LerpLog(float a, float b, float t)
        {
            float safeA = SafeVisibility(a);
            float safeB = SafeVisibility(b);
            return safeA * Mathf.Pow(safeB / safeA, t);
        }

        public static SkyState EvaluateSingle(WeatherPreset profile, float timeOfDay01)
        {
            if (profile == null)
                return default;

            // Земля больше не авторский градиент — тот же тон, что у горизонта, доля яркости
            // (см. .scratch/ambient-ground-derived/spec.md, решение #1). Выводится здесь, при
            // сборке состояния, а не в местах применения (решение #11) — обе копии применения
            // ambient (рантайм и Edit-Mode-превью) остаются нетронутыми.
            Color equatorColor = profile.AmbientEquatorColor.Evaluate(timeOfDay01);

            return new SkyState
            {
                ZenithColor = profile.SkyZenithColor.Evaluate(timeOfDay01),
                HorizonColor = profile.SkyHorizonColor.Evaluate(timeOfDay01),
                GradientExponent = profile.GradientExponent.Evaluate(timeOfDay01),

                CloudColor = profile.CloudColor.Evaluate(timeOfDay01),
                CloudSkyLitColor = profile.CloudSkyLitColor.Evaluate(timeOfDay01),
                CloudShadowColor = profile.CloudShadowColor.Evaluate(timeOfDay01),
                CloudHighlightColor = profile.CloudHighlightColor.Evaluate(timeOfDay01),
                CloudMoonColor = profile.CloudMoonColor.Evaluate(timeOfDay01),
                CloudCoverage = profile.CloudCoverage.Evaluate(timeOfDay01),
                CloudScale = profile.CloudScale,
                CloudSoftness = profile.CloudSoftness,
                WindSpeed = profile.WindSpeed,
                CloudRollBias = profile.CloudRollBias,
                CloudHighlightFalloff = profile.CloudHighlightFalloff,
                CloudMoonHighlightFalloff = profile.CloudMoonHighlightFalloff,
                CloudDetailScale = profile.CloudDetailScale,
                CloudDetailAmount = profile.CloudDetailAmount,
                CloudCohesion = profile.CloudCohesion,
                ShadowSampleDistance = profile.ShadowSampleDistance,
                ShadowDensity = profile.ShadowDensity,
                CloudThickness = profile.CloudThickness,
                BorderEffect = profile.BorderEffect,
                BorderHeight = profile.BorderHeight,
                CloudBorderColor = profile.CloudBorderColor.Evaluate(timeOfDay01),
                SkyLitSpread = profile.SkyLitSpread,
                SkyLitSoftness = profile.SkyLitSoftness,

                StormTint = profile.StormTint.Evaluate(timeOfDay01),
                StormCoverage = profile.StormCoverage.Evaluate(timeOfDay01),
                StormScale = profile.StormScale,
                StormThreshold = profile.StormThreshold,
                StormDirection = profile.StormDirection,
                StormFrontFalloff = profile.StormFrontFalloff,

                CirrusTint = profile.CirrusTint.Evaluate(timeOfDay01),
                CirrusCoverage = profile.CirrusCoverage.Evaluate(timeOfDay01),
                CirrusOpacity = profile.CirrusOpacity.Evaluate(timeOfDay01),
                CirrusScale = profile.CirrusScale,
                CirrusSpeed = profile.CirrusSpeed,

                AmbientSkyColor = profile.AmbientSkyColor.Evaluate(timeOfDay01),
                AmbientEquatorColor = equatorColor,
                AmbientGroundColor = equatorColor * profile.AmbientGroundReflectance,
                AmbientMultiplier = profile.AmbientMultiplier.Evaluate(timeOfDay01),

                SunIntensity = profile.SunIntensity.Evaluate(timeOfDay01),
                SunShadowStrength = profile.SunShadowStrength.Evaluate(timeOfDay01),
                SunColor = profile.SunColor.Evaluate(timeOfDay01),
                SunSize = profile.SunSize,
                SunHaloColor = profile.SunHaloColor.Evaluate(timeOfDay01),
                SunHaloFalloff = profile.SunHaloFalloff,
                SunHaloIntensity = profile.SunHaloIntensity.Evaluate(timeOfDay01),

                MoonIntensity = profile.MoonIntensity.Evaluate(timeOfDay01),
                MoonShadowStrength = profile.MoonShadowStrength.Evaluate(timeOfDay01),
                MoonColor = profile.MoonColor.Evaluate(timeOfDay01),
                MoonFlareFalloff = profile.MoonFlareFalloff,
                MoonFlareIntensity = profile.MoonFlareIntensity.Evaluate(timeOfDay01),

                StarColor = profile.StarColor.Evaluate(timeOfDay01),
                Latitude = profile.Latitude,

                SkyFogAmount = profile.SkyFogAmount.Evaluate(timeOfDay01),
                SkyFogHeight = profile.SkyFogHeight,
                SkyFogGlowSquish = profile.SkyFogGlowSquish,

                FogNearColor = profile.FogNearColor.Evaluate(timeOfDay01),
                FogMidColor = profile.FogMidColor.Evaluate(timeOfDay01),
                FogFarColor = profile.FogFarColor.Evaluate(timeOfDay01),
                FogMidPosition = profile.FogMidPosition,
                FogFarPosition = profile.FogFarPosition,
                FogVisibilityDistance = SafeVisibility(profile.FogVisibilityDistance.Evaluate(timeOfDay01)),
                CloudsFogAmount = profile.CloudsFogAmount.Evaluate(timeOfDay01),

                FilterColor = profile.FilterColor,
                FilterSaturation = profile.FilterSaturation,
                FilterValue = profile.FilterValue
            };
        }

        // Публичный: им пользуется WeatherComposer, накладывая зоны поверх собранного полосами.
        // Вторая копия этой арифметики дала бы расхождение и видимую ступеньку цвета на стыке.
        public static SkyState Lerp(SkyState a, SkyState b, float t) => new SkyState
        {
            ZenithColor = Color.Lerp(a.ZenithColor, b.ZenithColor, t),
            HorizonColor = Color.Lerp(a.HorizonColor, b.HorizonColor, t),
            GradientExponent = Mathf.Lerp(a.GradientExponent, b.GradientExponent, t),

            CloudColor = Color.Lerp(a.CloudColor, b.CloudColor, t),
            CloudSkyLitColor = Color.Lerp(a.CloudSkyLitColor, b.CloudSkyLitColor, t),
            CloudShadowColor = Color.Lerp(a.CloudShadowColor, b.CloudShadowColor, t),
            CloudHighlightColor = Color.Lerp(a.CloudHighlightColor, b.CloudHighlightColor, t),
            CloudMoonColor = Color.Lerp(a.CloudMoonColor, b.CloudMoonColor, t),
            CloudCoverage = Mathf.Lerp(a.CloudCoverage, b.CloudCoverage, t),
            CloudScale = Mathf.Lerp(a.CloudScale, b.CloudScale, t),
            CloudSoftness = Mathf.Lerp(a.CloudSoftness, b.CloudSoftness, t),
            WindSpeed = Mathf.Lerp(a.WindSpeed, b.WindSpeed, t),
            CloudRollBias = Mathf.Lerp(a.CloudRollBias, b.CloudRollBias, t),
            CloudHighlightFalloff = Mathf.Lerp(a.CloudHighlightFalloff, b.CloudHighlightFalloff, t),
            CloudMoonHighlightFalloff = Mathf.Lerp(a.CloudMoonHighlightFalloff, b.CloudMoonHighlightFalloff, t),
            CloudDetailScale = Mathf.Lerp(a.CloudDetailScale, b.CloudDetailScale, t),
            CloudDetailAmount = Mathf.Lerp(a.CloudDetailAmount, b.CloudDetailAmount, t),
            CloudCohesion = Mathf.Lerp(a.CloudCohesion, b.CloudCohesion, t),
            ShadowSampleDistance = Mathf.Lerp(a.ShadowSampleDistance, b.ShadowSampleDistance, t),
            ShadowDensity = Mathf.Lerp(a.ShadowDensity, b.ShadowDensity, t),
            CloudThickness = Mathf.Lerp(a.CloudThickness, b.CloudThickness, t),
            BorderEffect = Mathf.Lerp(a.BorderEffect, b.BorderEffect, t),
            BorderHeight = Mathf.Lerp(a.BorderHeight, b.BorderHeight, t),
            CloudBorderColor = Color.Lerp(a.CloudBorderColor, b.CloudBorderColor, t),
            SkyLitSpread = Mathf.Lerp(a.SkyLitSpread, b.SkyLitSpread, t),
            SkyLitSoftness = Mathf.Lerp(a.SkyLitSoftness, b.SkyLitSoftness, t),

            StormTint = Color.Lerp(a.StormTint, b.StormTint, t),
            StormCoverage = Mathf.Lerp(a.StormCoverage, b.StormCoverage, t),
            StormScale = Mathf.Lerp(a.StormScale, b.StormScale, t),
            StormThreshold = Mathf.Lerp(a.StormThreshold, b.StormThreshold, t),
            StormDirection = Vector3.Lerp(a.StormDirection, b.StormDirection, t),
            StormFrontFalloff = Mathf.Lerp(a.StormFrontFalloff, b.StormFrontFalloff, t),

            CirrusTint = Color.Lerp(a.CirrusTint, b.CirrusTint, t),
            CirrusCoverage = Mathf.Lerp(a.CirrusCoverage, b.CirrusCoverage, t),
            CirrusOpacity = Mathf.Lerp(a.CirrusOpacity, b.CirrusOpacity, t),
            CirrusScale = Mathf.Lerp(a.CirrusScale, b.CirrusScale, t),
            CirrusSpeed = Mathf.Lerp(a.CirrusSpeed, b.CirrusSpeed, t),

            AmbientSkyColor = Color.Lerp(a.AmbientSkyColor, b.AmbientSkyColor, t),
            AmbientEquatorColor = Color.Lerp(a.AmbientEquatorColor, b.AmbientEquatorColor, t),
            AmbientGroundColor = Color.Lerp(a.AmbientGroundColor, b.AmbientGroundColor, t),
            AmbientMultiplier = Mathf.Lerp(a.AmbientMultiplier, b.AmbientMultiplier, t),

            SunIntensity = Mathf.Lerp(a.SunIntensity, b.SunIntensity, t),
            SunShadowStrength = Mathf.Lerp(a.SunShadowStrength, b.SunShadowStrength, t),
            SunColor = Color.Lerp(a.SunColor, b.SunColor, t),
            SunSize = Mathf.Lerp(a.SunSize, b.SunSize, t),
            SunHaloColor = Color.Lerp(a.SunHaloColor, b.SunHaloColor, t),
            SunHaloFalloff = Mathf.Lerp(a.SunHaloFalloff, b.SunHaloFalloff, t),
            SunHaloIntensity = Mathf.Lerp(a.SunHaloIntensity, b.SunHaloIntensity, t),

            MoonIntensity = Mathf.Lerp(a.MoonIntensity, b.MoonIntensity, t),
            MoonShadowStrength = Mathf.Lerp(a.MoonShadowStrength, b.MoonShadowStrength, t),
            MoonColor = Color.Lerp(a.MoonColor, b.MoonColor, t),
            MoonFlareFalloff = Mathf.Lerp(a.MoonFlareFalloff, b.MoonFlareFalloff, t),
            MoonFlareIntensity = Mathf.Lerp(a.MoonFlareIntensity, b.MoonFlareIntensity, t),

            StarColor = Color.Lerp(a.StarColor, b.StarColor, t),
            Latitude = Mathf.Lerp(a.Latitude, b.Latitude, t),

            SkyFogAmount = Mathf.Lerp(a.SkyFogAmount, b.SkyFogAmount, t),
            SkyFogHeight = Mathf.Lerp(a.SkyFogHeight, b.SkyFogHeight, t),
            SkyFogGlowSquish = Mathf.Lerp(a.SkyFogGlowSquish, b.SkyFogGlowSquish, t),

            FogNearColor = Color.Lerp(a.FogNearColor, b.FogNearColor, t),
            FogMidColor = Color.Lerp(a.FogMidColor, b.FogMidColor, t),
            FogFarColor = Color.Lerp(a.FogFarColor, b.FogFarColor, t),
            FogMidPosition = Mathf.Lerp(a.FogMidPosition, b.FogMidPosition, t),
            FogFarPosition = Mathf.Lerp(a.FogFarPosition, b.FogFarPosition, t),
            // Единственное поле блендера, идущее не через Mathf.Lerp: линейный лерп по метрам
            // или по σ даёт разный результат в зависимости от того, что хранить, log-лерп —
            // один и тот же. Подробности и защита от нуля — в самом LerpLog.
            FogVisibilityDistance = LerpLog(a.FogVisibilityDistance, b.FogVisibilityDistance, t),
            CloudsFogAmount = Mathf.Lerp(a.CloudsFogAmount, b.CloudsFogAmount, t),

            FilterColor = Color.Lerp(a.FilterColor, b.FilterColor, t),
            FilterSaturation = Mathf.Lerp(a.FilterSaturation, b.FilterSaturation, t),
            FilterValue = Mathf.Lerp(a.FilterValue, b.FilterValue, t)
        };
    }
}
