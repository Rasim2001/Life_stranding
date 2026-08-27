using UnityEngine;

namespace WeatherSystem.Profiles
{
    // Пресет погоды — одно состояние небесных объектов: небо, облака, ambient, солнце, луна,
    // туман. Самостоятельный файл, назначаемый на любую высотную полосу рига, на любую зону
    // влияния и в любой сцене, сколько угодно раз: «закат над океаном» существует в одном
    // экземпляре и правится в одном месте.
    //
    // Высоты здесь нет намеренно. Полосы живут на WeatherRig и ссылаются сюда;
    // SkyBandBlender смешивает соседние полосы, а внутри каждой берёт суточное значение
    // через DailyColor/DailyFloat. См. .scratch/weather-presets-and-zones/spec.md, решения 1 и 6.
    //
    // Класс раньше назывался SkyBandProfile, а имя WeatherPreset носил тонкий держатель стека
    // полос. На втором грилле структура перевернулась, и файл переименован ВМЕСТЕ со своим
    // .cs.meta — GUID скрипта сохранён, поэтому существующие ассеты не потеряли ссылку.
    //
    // Границы [Range]/[DailyRange] здесь скопированы буквально из Properties шейдеров
    // (SHD_Weather_DomeSky/DomeClouds) — не подобраны на глаз. Исторический пример
    // (поле StarDensity к моменту написания этого комментария уже удалено, см.
    // .scratch/sky-night-and-star-dome/spec.md): значение на DATA_Weather_SkyBand_Above
    // авторски стояло 0.02, а "разумный" на вид диапазон [0, 0.01] тихо срезал бы его
    // до 0.01 при первом же касании слайдера — Unity не предупреждает о клампе, значение
    // просто становится другим. Перед тем как менять чьи-то границы здесь, свериться
    // с шейдером, не угадывать.
    [CreateAssetMenu(fileName = "DATA_Weather_Preset", menuName = "StaticData/Weather/Weather Preset")]
    public class WeatherPreset : ScriptableObject
    {
        [Header("Skydome")]
        public DailyColor SkyZenithColor;
        public DailyColor SkyHorizonColor;
        [DailyRange(0.1f, 8f)] public DailyFloat GradientExponent;

        // Форма облаков (Scale/Softness/WindSpeed/HighlightFalloff/Detail/Shadow-геометрия)
        // от времени суток не зависит — это художественная настройка носителя шума,
        // не освещения. Ведёт SHD_Weather_DomeClouds.shader.
        // Общий набор на все ярусы облаков разом (см. .scratch/cloud-color-architecture/spec.md,
        // Constraints and Decisions #1-2): освещённый/теневой/солнечная и лунная подсветка —
        // одна палитра на всё небо, каждый ярус ниже отклоняется от неё своим тинтом.
        // Отдельный цвет тени сохранён намеренно — единственный потребитель самозатенения,
        // тинт-множитель не смог бы сделать тень холоднее базы, только темнее.
        [Header("Clouds — shared")]
        public DailyColor CloudColor;
        // Небесная (скайлайт) сторона базы — противоположная солнцу. Реальный физический
        // источник другой: облака у солнца освещены прямым (на закате красным) светом,
        // с противоположной стороны — рассеянным скайлайтом (холодным). Альфа этого цвета,
        // не отдельный слайдер, задаёт силу подмеса — растёт сама к закату вместе с
        // остальным градиентом. См. .scratch/cloud-skylit-base-color/spec.md, решения #1-3.
        public DailyColor CloudSkyLitColor;
        public DailyColor CloudShadowColor;
        public DailyColor CloudHighlightColor;
        public DailyColor CloudMoonColor;
        [DailyRange(0f, 1f)] public DailyFloat CloudCoverage;
        // Частота базового 3D-шума по направлению — см. _CloudScale в SHD_Weather_DomeClouds.
        // Диапазон DomeClouds (0.1..10), не CloudLayer (0.001..0.05) — это два разных
        // свойства с одинаковым именем в разных шейдерах, поле кормит только купол.
        [Range(0.1f, 10f)] public float CloudScale = 2f;
        [Range(0.01f, 1f)] public float CloudSoftness = 0.18f;
        [Range(0f, 1f)] public float WindSpeed = 0.05f;
        // Перспектива плоского пласта у горизонта (закон секанса), не погодный фронт —
        // см. спек, Constraints and Decisions #17. Нельзя обнулять: при 0 небо физически
        // невозможно (зенит и горизонт одинаково облачны).
        [Range(0f, 1f)] public float CloudRollBias = 0.15f;
        [Range(1f, 64f)] public float CloudHighlightFalloff = 8f;
        // Ширина лунного ободка — простое число, не суточная кривая (спек, решение #10-11):
        // угловая ширина рассеяния на каплях от высоты светила не зависит. 22.9 — число Cozy
        // (cloudMoonHighlightFalloff), формула зеркальна уже проверенной солнечной.
        [Range(1f, 64f)] public float CloudMoonHighlightFalloff = 22.9f;
        [Range(0.1f, 10f)] public float CloudDetailScale = 1f;
        [Range(0f, 2f)] public float CloudDetailAmount = 1f;
        // Сплочённость: 0 — нарезка на отдельные комки как сейчас, 1 — слитная масса без
        // нарезки (спек, решение #13). 0.5 — не число Cozy (их Cohesion считается иначе),
        // середина диапазона по общему правилу захода — подбирает пользователь.
        [Range(0f, 1f)] public float CloudCohesion = 0.5f;
        [Range(0.01f, 1f)] public float ShadowSampleDistance = 0.30f;
        [Range(0f, 8f)] public float ShadowDensity = 3f;
        [Range(0f, 1f)] public float CloudThickness = 0.5f;
        [Range(0f, 1f)] public float BorderEffect = 0.35f;
        [Range(0.2f, 6f)] public float BorderHeight = 2f;
        // Цвет полосы света у горизонта — один, не направленный. Раньше был двухцветным
        // (тёплый к солнцу / холодный от него), но с появлением направленной базы
        // (CloudSkyLitColor выше) это дублировало один и тот же эффект дважды — border
        // вернулся к тому, чем задумывался: радиальная добавка одного цвета у горизонта.
        // См. .scratch/cloud-skylit-base-color/spec.md, решения #9-10.
        public DailyColor CloudBorderColor;
        // Порог направленной маски по sunDot: 0 — небесная сторона не проступает нигде,
        // 0.5 — ровно полусфера (противоположная солнцу), 1 — весь купол. Ось в косинусах,
        // не в углах (спек, решение #7) — 0.5 совпадает с полусферой точно, промежуточные
        // значения нелинейны по углу, цена arccos на пиксель не оправдана.
        [Range(0f, 1f)] public float SkyLitSpread = 0.5f;
        // Ширина перехода вокруг порога. Минимум не 0 — при совпадающих границах smoothstep
        // делит на ноль (тот же приём, что у CloudSoftness этого профиля).
        [Range(0.01f, 1f)] public float SkyLitSoftness = 0.15f;

        [Header("Clouds — storm")]
        // Тинт-множитель поверх общих цветов, не самостоятельный цвет (спек, решение #3, #5:
        // пользователю достаточно "темнее и синее"). Белый = неотличимо от кучевых.
        public DailyColor StormTint;
        // Независимое от CloudCoverage покрытие — выключатель грозы (спек, решение #8-11).
        // 0 = грозы нет никогда, ни при каких значениях остальных ярусов. Дефолт 0, не
        // середина диапазона: при текущих авторских значениях гроза и так не рисуется,
        // 0 воспроизводит это точно, а не "разумное" ненулевое значение.
        [DailyRange(0f, 1f)] public DailyFloat StormCoverage;
        [Range(0.1f, 10f)] public float StormScale = 1.3f;
        // Ниже какого покрытия грозовых нет вовсе; выше — набирают до единицы.
        [Range(0f, 1f)] public float StormThreshold = 0.55f;
        public Vector3 StormDirection = new Vector3(1f, 0.15f, 0f);
        [Range(0.5f, 16f)] public float StormFrontFalloff = 3f;

        [Header("Clouds — cirrus")]
        // Тинт-множитель, как у Storm — заменяет Cozy'шный "High Altitude Cloud Color"
        // (у них тоже множитель поверх базы, см. спек, Further Notes).
        public DailyColor CirrusTint;
        // Независимая ручка: перистые бывают и на чистом небе, к CloudCoverage не привязаны.
        [DailyRange(0f, 1f)] public DailyFloat CirrusCoverage;
        [DailyRange(0f, 1f)] public DailyFloat CirrusOpacity;
        [Range(0.1f, 10f)] public float CirrusScale = 3f;
        [Range(0f, 1f)] public float CirrusSpeed = 0.12f;

        [Header("Ambient")]
        public DailyColor AmbientSkyColor;
        public DailyColor AmbientEquatorColor;
        // Цвет земли больше не самостоятельный градиент — выводится из AmbientEquatorColor
        // этим скаляром (см. .scratch/ambient-ground-derived/spec.md, решение #1-3): земля
        // это отражённый свет горизонта, тот же тон, доля яркости. Простое число, не суточная
        // кривая — отражение это свойство грунта, не времени (решение #4). Дефолт 0.5 —
        // число Cozy (Color.gray), не выведенное из авторских значений (решение #5).
        [Range(0f, 1f)] public float AmbientGroundReflectance = 0.5f;
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

        // Ночь больше не отдельный механизм (был NightFactor от высоты солнца + NightTint
        // поверх градиента) — она теперь просто тёмный конец SkyZenithColor/SkyHorizonColor,
        // как у Cozy (см. .scratch/sky-night-and-star-dome/spec.md, решение #1). Звёзды —
        // текстура звёздной карты на вращающейся сфере, а не процедурный шум; яркость
        // ведётся тем же суточным градиентом, что и небо, а не отдельным ночным порогом.
        [Header("Night")]
        public DailyColor StarColor;
        // Наклон звёздной сферы. 0 — середина диапазона: у Cozy нет отдельного поля
        // "широта" (там наклон завязан на их собственную систему позиционирования солнца),
        // сопоставимого числа нет, дефолт по общему правилу захода.
        [Range(-90f, 90f)] public float Latitude;

        // Цвет дымки больше не свой (SkyFogColor удалён) — купол берёт его из дальнего
        // стопа тумана, FogFarColor ниже (спек, решение #13). Две ручки "сколько" и
        // "докуда по высоте" остаются собственными: сила и форма дымки — художественные
        // настройки купола, а не тумана.
        [Header("Horizon fog")]
        [DailyRange(0f, 1f)] public DailyFloat SkyFogAmount;
        [Range(0.01f, 1f)] public float SkyFogHeight = 0.18f;
        [Range(0.1f, 6f)] public float SkyFogGlowSquish = 2.5f;

        // Туман на мировой геометрии — рампа и плотность свои, но цвет дальнего стопа
        // общий с дымкой купола выше и с подмесом в облака ниже (см. .scratch/
        // fog-generation-and-layers/spec.md, решения #1-6, #13-15). Непрозрачность
        // считает экспонента от дальности видимости, три цвета — художественная рампа поверх.
        [Header("Distance fog")]
        public DailyColor FogNearColor;
        public DailyColor FogMidColor;
        public DailyColor FogFarColor;
        // Доли дальности видимости, не метры (решение #3) — при смене плотности палитра
        // остаётся читаемой. >1 разрешено осознанно: отодвигает дальний стоп, не трогая
        // плотность. Верхняя граница слайдера 4 — запас для этого случая, не хард-лимит
        // (можно ввести значение больше вручную).
        [Range(0f, 4f)] public float FogMidPosition = 0.35f;
        [Range(0f, 4f)] public float FogFarPosition = 1f;
        // Границы 5..2000 м: 2000 — дальняя плоскость отсечения, уже используемая в сценах
        // проекта; 5 держит σ=3/V конечной ниже авторского референса "густой туман" (15 м).
        // Не окончательно — спек прямо просит уточнить после первого визуального прохода.
        [DailyRange(5f, 2000f, Logarithmic = true)] public DailyFloat FogVisibilityDistance;
        // Насколько облачный купол тонет в цвете дальнего стопа (решение #14) — своя ручка,
        // не производная от SkyFogAmount: у купола облаков и купола неба разная сила
        // растворения по художественному замыслу, только форма (по высоте) общая.
        [DailyRange(0f, 1f)] public DailyFloat CloudsFogAmount;

        // Одна ручка на всё небо купола разом (и Sky, и Clouds получают одни значения
        // из WeatherService.ApplySky) — обесцветить/притемнить под грозу без правки
        // десятка цветов по отдельности. Не суточная: это состояние погоды, не времени.
        [Header("Weather filter")]
        public Color FilterColor = Color.white;
        [Range(-1f, 1f)] public float FilterSaturation;
        [Range(-1f, 1f)] public float FilterValue;
    }
}
