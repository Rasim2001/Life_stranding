using UnityEngine;

namespace WeatherSystem.Profiles
{
    // Состояние купола неба на одной высотной полосе. WeatherStaticData.Bands ссылается
    // на несколько таких профилей по высоте; SkyBandBlender смешивает соседние полосы
    // и внутри каждой вычисляет суточное значение через DailyColor/DailyFloat.
    //
    // Границы [Range]/[DailyRange] здесь скопированы буквально из Properties шейдеров
    // (SHD_Weather_DomeSky/DomeClouds) — не подобраны на глаз. Причина: значение
    // StarDensity на DATA_Weather_SkyBand_Above авторски стоит 0.02, а "разумный" на вид
    // диапазон [0, 0.01] тихо срезал бы его до 0.01 (2/3 звёзд) при первом же касании
    // слайдера — Unity не предупреждает о клампе, значение просто становится другим.
    // Перед тем как менять чьи-то границы здесь, свериться с шейдером, не угадывать.
    [CreateAssetMenu(fileName = "DATA_Weather_SkyBand", menuName = "StaticData/Weather/Sky Band Profile")]
    public class SkyBandProfile : ScriptableObject
    {
        [Header("Skydome")]
        public DailyColor SkyZenithColor;
        public DailyColor SkyHorizonColor;
        [DailyRange(0.1f, 8f)] public DailyFloat GradientExponent;

        // Форма облаков (Scale/Softness/WindSpeed/HighlightFalloff/Detail/Shadow-геометрия)
        // от времени суток не зависит — это художественная настройка носителя шума,
        // не освещения. Ведёт SHD_Weather_DomeClouds.shader.
        [Header("Clouds — cumulus")]
        public DailyColor CloudColor;
        public DailyColor CloudShadowColor;
        public DailyColor CloudHighlightColor;
        [DailyRange(0f, 1f)] public DailyFloat CloudCoverage;
        // Частота базового 3D-шума по направлению — см. _CloudScale в SHD_Weather_DomeClouds.
        // Диапазон DomeClouds (0.1..10), не CloudLayer (0.001..0.05) — это два разных
        // свойства с одинаковым именем в разных шейдерах, поле кормит только купол.
        [Range(0.1f, 10f)] public float CloudScale = 2f;
        [Range(0.01f, 1f)] public float CloudSoftness = 0.18f;
        [Range(0f, 1f)] public float WindSpeed = 0.05f;
        [Range(0f, 1f)] public float CloudRollBias = 0.15f;
        [Range(1f, 64f)] public float CloudHighlightFalloff = 8f;
        [Range(0.1f, 10f)] public float CloudDetailScale = 1f;
        [Range(0f, 2f)] public float CloudDetailAmount = 1f;
        [Range(0.01f, 1f)] public float ShadowSampleDistance = 0.30f;
        [Range(0f, 8f)] public float ShadowDensity = 3f;
        [Range(0f, 1f)] public float CloudThickness = 0.5f;
        [Range(0f, 1f)] public float BorderEffect = 0.35f;
        [Range(0.2f, 6f)] public float BorderHeight = 2f;

        [Header("Clouds — storm")]
        public DailyColor StormColor;
        public DailyColor StormShadowColor;
        [Range(0.1f, 10f)] public float StormScale = 1.3f;
        // Ниже какого покрытия грозовых нет вовсе; выше — набирают до единицы.
        [Range(0f, 1f)] public float StormThreshold = 0.55f;
        public Vector3 StormDirection = new Vector3(1f, 0.15f, 0f);
        [Range(0.5f, 16f)] public float StormFrontFalloff = 3f;

        [Header("Clouds — cirrus")]
        public DailyColor CirrusColor;
        // Независимая ручка: перистые бывают и на чистом небе, к CloudCoverage не привязаны.
        [DailyRange(0f, 1f)] public DailyFloat CirrusCoverage;
        [DailyRange(0f, 1f)] public DailyFloat CirrusOpacity;
        [Range(0.1f, 10f)] public float CirrusScale = 3f;
        [Range(0f, 1f)] public float CirrusSpeed = 0.12f;

        [Header("Ambient")]
        public DailyColor AmbientSkyColor;
        public DailyColor AmbientEquatorColor;
        public DailyColor AmbientGroundColor;
        // Одна ручка яркости на все три ambient-цвета разом (как ambientLightMultiplier
        // у Cozy) — умножает только RGB, альфа ambient-цветов Unity не читает.
        // Дефолт DailyFloat._constant — 0, поэтому на существующих ассетах после
        // добавления поля значение накатывается одноразовым скриптом
        // (Assets/Editor/Weather/WeatherTimeMigration.cs), а не полагается на
        // C#-инициализатор: Unity не гарантирует, что он отработает при десериализации
        // уже существующего .asset (может быть GetUninitializedObject в обход конструктора).
        // Границы не из шейдера (RenderSettings, не свойство материала) — у кози их
        // собственный ambientLightMultiplier тоже 0..2, повторяем.
        [DailyRange(0f, 2f)] public DailyFloat AmbientMultiplier;

        // SunColor/MoonColor двойного назначения: красят и Light-источник
        // (WeatherService.ApplySun/ApplyMoon), и диск/гало в SHD_Weather_DomeSky —
        // одна ручка на "какого цвета сейчас светило", а не две рассинхронизированные.
        [Header("Sun")]
        // Границы не из шейдера — это Light.intensity, не свойство материала. Авторски
        // на DATA_Weather_SkyBand_Above уже стоит 1.6, поэтому потолок НЕ 0..1 (срезал бы
        // 37% света у самой верхней полосы) — 8 соответствует практике URP directional light.
        [DailyRange(0f, 8f)] public DailyFloat SunIntensity;
        public DailyColor SunColor;
        [Range(0.5f, 8f)] public float SunSize = 1.3f;
        public DailyColor SunHaloColor;
        [Range(0f, 1f)] public float SunHaloFalloff = 0.15f;
        [DailyRange(0f, 3f)] public DailyFloat SunHaloIntensity;

        [Header("Moon")]
        // Границы не из шейдера — Light.intensity. Авторский максимум сейчас 0.22, 2 — запас.
        [DailyRange(0f, 2f)] public DailyFloat MoonIntensity;
        public DailyColor MoonColor;
        [Range(0f, 1f)] public float MoonFlareFalloff = 0.5f;
        [DailyRange(0f, 3f)] public DailyFloat MoonFlareIntensity;

        // Не суточные: звёзды и ночной тинт видны только когда NightFactor уже почти 1,
        // добавлять им ещё и суточный градиент — вводить вторую ручку на то же состояние.
        [Header("Night")]
        [Range(0f, 0.05f)] public float StarDensity = 0.006f;
        public Color NightTint = new Color(0.12f, 0.14f, 0.28f);

        [Header("Horizon fog")]
        public DailyColor SkyFogColor;
        [DailyRange(0f, 1f)] public DailyFloat SkyFogAmount;
        [Range(0.01f, 1f)] public float SkyFogHeight = 0.18f;
        [Range(0.1f, 6f)] public float SkyFogGlowSquish = 2.5f;

        // Одна ручка на всё небо купола разом (и Sky, и Clouds получают одни значения
        // из WeatherService.ApplySky) — обесцветить/притемнить под грозу без правки
        // десятка цветов по отдельности. Не суточная: это состояние погоды, не времени.
        [Header("Weather filter")]
        public Color FilterColor = Color.white;
        [Range(-1f, 1f)] public float FilterSaturation;
        [Range(-1f, 1f)] public float FilterValue;
    }
}
