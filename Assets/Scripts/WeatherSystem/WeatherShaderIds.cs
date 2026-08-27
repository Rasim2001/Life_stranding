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
        public static readonly int StarColor = Shader.PropertyToID("_StarColor");
        public static readonly int Latitude = Shader.PropertyToID("_Latitude");
        public static readonly int Alpha = Shader.PropertyToID("_Alpha");

        public static readonly int CloudColor = Shader.PropertyToID("_CloudColor");
        public static readonly int CloudSkyLitColor = Shader.PropertyToID("_CloudSkyLitColor");
        public static readonly int CloudShadowColor = Shader.PropertyToID("_CloudShadowColor");
        public static readonly int CloudHighlightColor = Shader.PropertyToID("_CloudHighlightColor");
        public static readonly int CloudMoonColor = Shader.PropertyToID("_CloudMoonColor");
        public static readonly int CloudCoverage = Shader.PropertyToID("_CloudCoverage");
        public static readonly int CloudScale = Shader.PropertyToID("_CloudScale");
        public static readonly int CloudSoftness = Shader.PropertyToID("_CloudSoftness");
        public static readonly int WindSpeed = Shader.PropertyToID("_WindSpeed");
        public static readonly int CloudRollBias = Shader.PropertyToID("_CloudRollBias");
        public static readonly int CloudHighlightFalloff = Shader.PropertyToID("_CloudHighlightFalloff");
        public static readonly int CloudMoonHighlightFalloff = Shader.PropertyToID("_CloudMoonHighlightFalloff");
        public static readonly int CloudDetailScale = Shader.PropertyToID("_CloudDetailScale");
        public static readonly int CloudDetailAmount = Shader.PropertyToID("_CloudDetailAmount");
        public static readonly int CloudCohesion = Shader.PropertyToID("_CloudCohesion");
        public static readonly int ShadowSampleDistance = Shader.PropertyToID("_ShadowSampleDistance");
        public static readonly int ShadowDensity = Shader.PropertyToID("_ShadowDensity");
        public static readonly int CloudThickness = Shader.PropertyToID("_CloudThickness");
        public static readonly int BorderEffect = Shader.PropertyToID("_BorderEffect");
        public static readonly int BorderHeight = Shader.PropertyToID("_BorderHeight");
        public static readonly int CloudBorderColor = Shader.PropertyToID("_CloudBorderColor");
        public static readonly int SkyLitSpread = Shader.PropertyToID("_SkyLitSpread");
        public static readonly int SkyLitSoftness = Shader.PropertyToID("_SkyLitSoftness");

        public static readonly int StormTint = Shader.PropertyToID("_StormTint");
        public static readonly int StormCoverage = Shader.PropertyToID("_StormCoverage");
        public static readonly int StormScale = Shader.PropertyToID("_StormScale");
        public static readonly int StormThreshold = Shader.PropertyToID("_StormThreshold");
        public static readonly int StormDirection = Shader.PropertyToID("_StormDirection");
        public static readonly int StormFrontFalloff = Shader.PropertyToID("_StormFrontFalloff");

        public static readonly int CirrusTint = Shader.PropertyToID("_CirrusTint");
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

        // Цвет дымки больше не своё свойство (SkyFogColor удалён) — оба купольных шейдера
        // читают его глобалом _SR_FogFarColor (см. блок глобалов ниже), тем же, что и
        // дальний стоп тумана.
        public static readonly int SkyFogAmount = Shader.PropertyToID("_SkyFogAmount");
        public static readonly int SkyFogHeight = Shader.PropertyToID("_SkyFogHeight");
        public static readonly int SkyFogGlowSquish = Shader.PropertyToID("_SkyFogGlowSquish");
        public static readonly int CloudsFogAmount = Shader.PropertyToID("_CloudsFogAmount");

        public static readonly int FilterColor = Shader.PropertyToID("_FilterColor");
        public static readonly int FilterSaturation = Shader.PropertyToID("_FilterSaturation");
        public static readonly int FilterValue = Shader.PropertyToID("_FilterValue");

        // Глобалы (Shader.SetGlobalVector/Float), не свойства материала — направления на
        // светила и время суток нужны не только небу, но и облакам, позже туману и воде.
        public static readonly int SunDirectionGlobal = Shader.PropertyToID("_SR_SunDirection");
        public static readonly int MoonDirectionGlobal = Shader.PropertyToID("_SR_MoonDirection");
        public static readonly int TimeOfDay01Global = Shader.PropertyToID("_SR_TimeOfDay01");

        // Позиция камеры для потребителей, которым недоступна штатная _WorldSpaceCameraPos —
        // сейчас единственный: BlendStylizedFog воды (WeatherWaterFog.hlsl), подключаемый
        // Stylized Water 3 текстово раньше собственных Libraries/Input.hlsl, где объявлена
        // штатная переменная (см. комментарий в WeatherWaterFog.hlsl — проверено эмпирически,
        // и "undeclared identifier" при использовании штатной, и "redefinition" при попытке
        // объявить её самим). Кормится из WeatherService.Tick() через ICameraProviderService.
        public static readonly int CameraPositionGlobal = Shader.PropertyToID("_SR_CameraPositionWS");

        // Туман на мировой геометрии — тоже глобалы, не per-material свойства: у fullscreen-
        // пасса нет своего материала-получателя через IWeatherPropertySink, а вода/стекло/VFX
        // в следующих тикетах читают их напрямую из шейдера. См. WeatherFogApplier.
        public static readonly int FogVisibilityDistanceGlobal = Shader.PropertyToID("_SR_FogVisibilityDistance");
        public static readonly int FogNearColorGlobal = Shader.PropertyToID("_SR_FogNearColor");
        public static readonly int FogMidColorGlobal = Shader.PropertyToID("_SR_FogMidColor");
        public static readonly int FogFarColorGlobal = Shader.PropertyToID("_SR_FogFarColor");
        public static readonly int FogMidPositionGlobal = Shader.PropertyToID("_SR_FogMidPosition");
        public static readonly int FogFarPositionGlobal = Shader.PropertyToID("_SR_FogFarPosition");

        // Погодный фильтр как глобал — те же значения, что FilterColor/Saturation/Value выше,
        // для потребителей без материала-получателя (см. комментарий блока Fog).
        public static readonly int FilterColorGlobal = Shader.PropertyToID("_SR_FilterColor");
        public static readonly int FilterSaturationGlobal = Shader.PropertyToID("_SR_FilterSaturation");
        public static readonly int FilterValueGlobal = Shader.PropertyToID("_SR_FilterValue");

        // Не через IWeatherPropertySink: фиксированный арт-ассет, не суточная величина —
        // назначается один раз на общий материал, не пересылается каждый кадр.
        public static readonly int StarDomeTexture = Shader.PropertyToID("_StarDomeTexture");
    }
}
