using UnityEngine;
using WeatherSystem.Profiles;

namespace WeatherSystem
{
    // Дальностный туман — глобалы шейдера (Shader.SetGlobalX), не свойства материала:
    // у fullscreen-пасса тумана нет своего материала, через который WeatherSkyApplier
    // мог бы их доставить, а водные/стеклянные/VFX-материалы следующих тикетов читают
    // глобалы напрямую. Единая точка вызова у рантайма и Edit Mode превью — как и у
    // WeatherSkyApplier, чтобы девять полей не разъехались между двумя вызывающими.
    //
    // Глобалы — статическое состояние шейдера: они переживают выгрузку сцены и выход из
    // Play Mode. Поэтому у ApplyGlobals есть парный ResetGlobals, и звать его обязаны все,
    // кто перестаёт вести туман (WeatherService.Dispose, WeatherPreviewDriver.Stop) —
    // иначе фича тумана, зарегистрированная в рендерере глобально, продолжит красить
    // кадр значениями от предыдущего уровня или от уже остановленного превью.
    public static class WeatherFogApplier
    {
        // Засев при загрузке. Купольные шейдеры читают _SR_FogFarColor БЕЗУСЛОВНО (цвет
        // горизонтной дымки, .rgb без учёта альфы), а не только когда туман кому-то нужен.
        // Незасеянный глобал равен нулю, то есть чёрному: у MAT_Weather_DomeSky авторски
        // стоит _SkyFogAmount = 1, поэтому до первого Tick/Apply горизонт красился в чёрный —
        // в редакторе постоянно (пока не запустишь превью или Play Mode), в билде один кадр.
        // Раньше этого не было: цвет жил свойством материала с сериализованным дефолтом.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        private static void SeedGlobals() => ResetGlobals();

        // cameraPositionWS — обязательный параметр, а не отдельный метод: позиция камеры
        // такой же глобал этой системы, как и всё остальное здесь, и её единственный
        // потребитель (туман на воде) ломается молча, если её забыть протолкнуть. Обязательный
        // аргумент делает «забыть» невозможным — ровно та ошибка, что уже случилась однажды.
        // Nullable, потому что вызывающий честно может её не знать: у WeatherService камера
        // приезжает только с BuildLevelState. В этом случае глобал не трогаем — пусть держит
        // засеянное значение, а не нуль, выданный за настоящую позицию.
        public static void ApplyGlobals(SkyState sky, Vector3? cameraPositionWS)
        {
            if (cameraPositionWS.HasValue)
                Shader.SetGlobalVector(WeatherShaderIds.CameraPositionGlobal, cameraPositionWS.Value);

            // Последний шлюз перед шейдером: сюда доезжает и default(SkyState) — пустой
            // массив полос или полоса без Profile, — где дальность видимости равна нулю
            // и ни один SafeVisibility в блендере не отрабатывал. Ноль здесь дал бы
            // σ = 3/0.001 = 3000, то есть сплошную пелену на весь мир.
            Shader.SetGlobalFloat(WeatherShaderIds.FogVisibilityDistanceGlobal,
                SkyBandBlender.SafeVisibility(sky.FogVisibilityDistance));
            Shader.SetGlobalColor(WeatherShaderIds.FogNearColorGlobal, sky.FogNearColor);
            Shader.SetGlobalColor(WeatherShaderIds.FogMidColorGlobal, sky.FogMidColor);
            Shader.SetGlobalColor(WeatherShaderIds.FogFarColorGlobal, sky.FogFarColor);
            Shader.SetGlobalFloat(WeatherShaderIds.FogMidPositionGlobal, sky.FogMidPosition);
            Shader.SetGlobalFloat(WeatherShaderIds.FogFarPositionGlobal, sky.FogFarPosition);

            Shader.SetGlobalColor(WeatherShaderIds.FilterColorGlobal, sky.FilterColor);
            Shader.SetGlobalFloat(WeatherShaderIds.FilterSaturationGlobal, sky.FilterSaturation);
            Shader.SetGlobalFloat(WeatherShaderIds.FilterValueGlobal, sky.FilterValue);
        }

        // Состояние "тумана нет". Альфа цветов — ноль, поэтому finalAlpha в шейдере фога
        // равен нулю независимо от остального. FogFarColor — исключение: с тикета 02 его
        // .rgb безусловно (без учёта альфы) читают ApplyHorizonFog в куполе неба и подмес
        // в куполе облаков, поэтому Color.clear покрасил бы горизонт в чёрный на любой
        // полосе с ненулевым SkyFogAmount/CloudsFogAmount. Белый с нулевой альфой держит
        // оба смысла разом: дальностный туман выключен (альфа), а купол не чернеет (rgb).
        public static void ResetGlobals()
        {
            Shader.SetGlobalFloat(WeatherShaderIds.FogVisibilityDistanceGlobal, SkyBandBlender.NoFogVisibility);
            Shader.SetGlobalColor(WeatherShaderIds.FogNearColorGlobal, Color.clear);
            Shader.SetGlobalColor(WeatherShaderIds.FogMidColorGlobal, Color.clear);
            Shader.SetGlobalColor(WeatherShaderIds.FogFarColorGlobal, new Color(1f, 1f, 1f, 0f));
            Shader.SetGlobalFloat(WeatherShaderIds.FogMidPositionGlobal, 0f);
            Shader.SetGlobalFloat(WeatherShaderIds.FogFarPositionGlobal, 1f);

            Shader.SetGlobalColor(WeatherShaderIds.FilterColorGlobal, Color.white);
            Shader.SetGlobalFloat(WeatherShaderIds.FilterSaturationGlobal, 0f);
            Shader.SetGlobalFloat(WeatherShaderIds.FilterValueGlobal, 0f);

            // Позиция камеры в состоянии "тумана нет" ни на что не влияет: альфа цветов
            // нулевая, дальность видимости — NoFogVisibility, поэтому итоговая плотность
            // равна нулю при любой дистанции. Обнуляем не ради картинки, а чтобы не оставлять
            // позицию камеры предыдущего уровня следующей сцене.
            Shader.SetGlobalVector(WeatherShaderIds.CameraPositionGlobal, Vector3.zero);
        }
    }
}
