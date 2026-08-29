using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using WeatherSystem;
using WeatherSystem.Profiles;

namespace SpiderRig.Editor.Weather
{
    // Edit Mode превью погоды — воспроизводит порядок WeatherService.Tick() (поворот
    // пивота → SkyState → небо/облака/ambient/светила), но пишет только туда, откуда
    // можно откатиться:
    //
    //  - купола и луна — через MaterialPropertyBlock, не sharedMaterial. Материал-ассет
    //    не трогается вообще (см. предупреждение в WeatherService.cs:65-68 про то, как
    //    запись в общий материал каждый кадр портит ассет и приезжает паразитным диффом
    //    в git) — Stop() просто снимает блок, без "восстановления" чего-либо;
    //  - RenderSettings.ambient*, Light.intensity/color/enabled и поворот пивота реально
    //    меняют состояние сцены — их снимаем в Start() и возвращаем в Stop().
    //
    // Список свойств шейдера общий с рантаймом — WeatherSkyApplier.Apply, единая точка,
    // см. её комментарий.
    public class WeatherPreviewDriver
    {
        private readonly MaterialPropertyBlock _skyBlock = new MaterialPropertyBlock();
        private readonly MaterialPropertyBlock _cloudsBlock = new MaterialPropertyBlock();
        private readonly MaterialPropertyBlock _moonBlock = new MaterialPropertyBlock();

        private WeatherRig _rig;
        private bool _running;

        private AmbientMode _snapAmbientMode;
        private Light _snapRenderSettingsSun;
        private Color _snapAmbientSky;
        private Color _snapAmbientEquator;
        private Color _snapAmbientGround;
        private float _snapSunIntensity;
        private Color _snapSunColor;
        private bool _snapSunEnabled;
        private float _snapMoonIntensity;
        private Color _snapMoonColor;
        private bool _snapMoonEnabled;
        private Vector3 _snapPivotEuler;

        // Авторская высота облачного слоя. NaN — «снимка нет», тогда откат позиции просто
        // оставит префабное значение.
        private float _snapCloudLayerY = float.NaN;

        public bool IsRunning => _running;
        public WeatherRig Rig => _rig;

        public void Start(WeatherRig rig)
        {
            if (_running)
                Stop();

            _rig = rig;
            TakeSnapshot();
            _running = true;
        }

        public void Stop()
        {
            if (!_running)
                return;

            // Риг мог быть уничтожен вместе с выгруженной сценой. Восстанавливать по снимку
            // тогда нельзя: RenderSettings.ambient* глобальны, и мы записали бы значения
            // прежней сцены в текущую. Блоки и глобалы тумана снять всё равно надо.
            if (_rig == null)
            {
                WeatherFogApplier.ResetGlobals();
                _running = false;
                return;
            }

            RestoreSnapshot();

            ClearBlock(_rig.SkyDome);
            ClearBlock(_rig.CloudsDome);
            ClearBlock(_rig.Moon);

            // Туман идёт глобалами, а не MaterialPropertyBlock'ом — снятием блока он не
            // убирается. Без явного сброса Scene view остался бы затуманенным после Stop,
            // хотя весь смысл этого класса в том, чтобы превью не оставляло следов.
            WeatherFogApplier.ResetGlobals();

            _running = false;
            _rig = null;
        }

        // Откат авторских значений перед записью сцены на диск. Погода — производное состояние,
        // и в .unity ей делать нечего: замер показал, что при сохранении туда попадают и
        // RenderSettings.ambient*, и intensity/color света, и поворот пивота — все три разом.
        // Драйвер не останавливается: следующий Apply вернёт картинку, зритель ничего не заметит.
        public void RestoreSceneValues()
        {
            if (!_running || _rig == null)
                return;

            RestoreSnapshot();
            RevertDerivedPrefabOverrides();
        }

        // Снять привязку, ничего не восстанавливая. Нужно при загрузке новой сцены: снимок
        // относится к прежней, и «восстановление» им означало бы записать чужие значения
        // в свежую сцену — а следующий Start() снял бы снимок уже с этой порчи. Измерено:
        // именно так авторский ambient подменялся ведомым.
        public void Abandon()
        {
            if (!_running)
                return;

            if (_rig != null)
            {
                ClearBlock(_rig.SkyDome);
                ClearBlock(_rig.CloudsDome);
                ClearBlock(_rig.Moon);
            }

            WeatherFogApplier.ResetGlobals();

            _running = false;
            _rig = null;
        }

        // Второй слой отката, без которого сохранение всё равно течёт.
        //
        // Ключевой факт, проверенный опытом: **вернуть префабное значение недостаточно**.
        // Unity ведёт список модификаций инстанса по факту записи, а не сравнением значений,
        // поэтому после присваивания «того же самого» в .unity всё равно уезжает запись
        // оверрайда — просто со значением, равным префабному. Удалять надо саму запись.
        //
        // Затереть чей-то осмысленный оверрайд мы этим не можем: все перечисленные свойства
        // компоненты перезаписывают каждый апдейт (WeatherDome, WeatherMoon и CloudLayer
        // помечены [ExecuteAlways] и двигают себя сами), значит оверрайд там по построению
        // производный. Единственное исключение — высота облачного слоя, она авторская,
        // и её возвращаем отдельно после отката.
        private void RevertDerivedPrefabOverrides()
        {
            RevertTransformOverrides(_rig.SkyDome != null ? _rig.SkyDome.transform : null);
            RevertTransformOverrides(_rig.CloudsDome != null ? _rig.CloudsDome.transform : null);
            RevertTransformOverrides(_rig.Moon != null ? _rig.Moon.transform : null);
            RevertTransformOverrides(_rig.CloudLayer != null ? _rig.CloudLayer.transform : null);
            RevertTransformOverrides(_rig.SunMoonPivot);

            // С среза 2 повороты ламп сами стали производными (время едет на них, не на
            // пивоте) — без отката сюда потёк бы новый оверрайд на каждый кадр превью,
            // ровно как у купольных трансформов выше.
            RevertTransformOverrides(_rig.SunLight != null ? _rig.SunLight.transform : null);
            RevertTransformOverrides(_rig.MoonLight != null ? _rig.MoonLight.transform : null);

            RevertLightOverrides(_rig.SunLight);
            RevertLightOverrides(_rig.MoonLight);

            RestoreAuthoredCloudLayerHeight();
        }

        // Высота облачного слоя задаётся размещением в сцене — это авторский per-scene
        // оверрайд, ровно как пороги высоты на риге (см. комментарий в CloudLayer).
        // Откат позиции сносит её вместе с производными X/Z, поэтому возвращаем её обратно.
        private void RestoreAuthoredCloudLayerHeight()
        {
            if (_rig.CloudLayer == null || float.IsNaN(_snapCloudLayerY))
                return;

            Transform layer = _rig.CloudLayer.transform;
            if (Mathf.Approximately(layer.localPosition.y, _snapCloudLayerY))
                return;

            Vector3 position = layer.localPosition;
            position.y = _snapCloudLayerY;
            layer.localPosition = position;
        }

        private static void RevertTransformOverrides(Transform target)
        {
            RevertOverride(target, "m_LocalPosition");
            RevertOverride(target, "m_LocalRotation");
            RevertOverride(target, "m_LocalScale");
        }

        private static void RevertLightOverrides(Light target)
        {
            RevertOverride(target, "m_Enabled");
            RevertOverride(target, "m_Intensity");
            RevertOverride(target, "m_Color");
            // Путь именно такой — m_ShadowStrength не существует, проверено перебором
            // SerializedObject на живой лампе. Валидные: m_Shadows.m_Type, m_Shadows.m_Strength.
            RevertOverride(target, "m_Shadows.m_Strength");
            RevertOverride(target, "m_Shadows.m_Type");
        }

        private static void RevertOverride(Object target, string propertyPath)
        {
            if (target == null || !PrefabUtility.IsPartOfPrefabInstance(target))
                return;

            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyPath);
            if (property == null || !property.prefabOverride)
                return;

            PrefabUtility.RevertPropertyOverride(property, InteractionMode.AutomatedAction);
        }

        // Готовое состояние параметром, а не пресет: как его собрать — из полос рига по высоте
        // или из одного пресета плоско, когда открыто окно, — решает вызывающий. Драйвер
        // только применяет, как и WeatherService в рантайме.
        public void Apply(SkyState sky, float timeOfDay01)
        {
            if (!_running || _rig == null)
                return;

            RotateSunMoonPivot(timeOfDay01);

            float sunElevation = GetSunElevation();

            ApplyToDome(_rig.SkyDome, _skyBlock, sky);
            ApplyToDome(_rig.CloudsDome, _cloudsBlock, sky);
            ApplyAmbient(sky);
            ApplySun(sky, sunElevation);
            ApplyMoon(sky);
            // Обязателен: без него fullscreen-пасс тумана не видит изменений панели,
            // и Edit Mode превью тумана невозможно в принципе (спек, Verification).
            // Позиция камеры — камера Scene view: в Edit Mode ICameraProviderService пуст
            // (он заполняется в BuildLevelState, то есть только в Play Mode), а без неё
            // туман на воде считался бы от начала координат и не реагировал на облёт сцены.
            WeatherFogApplier.ApplyGlobals(sky, GetSceneViewCameraPositionWS());
        }

        private void TakeSnapshot()
        {
            _snapAmbientMode = RenderSettings.ambientMode;
            _snapRenderSettingsSun = RenderSettings.sun;
            _snapAmbientSky = RenderSettings.ambientSkyColor;
            _snapAmbientEquator = RenderSettings.ambientEquatorColor;
            _snapAmbientGround = RenderSettings.ambientGroundColor;

            if (_rig.SunLight != null)
            {
                _snapSunIntensity = _rig.SunLight.intensity;
                _snapSunColor = _rig.SunLight.color;
                _snapSunEnabled = _rig.SunLight.enabled;
            }

            if (_rig.MoonLight != null)
            {
                _snapMoonIntensity = _rig.MoonLight.intensity;
                _snapMoonColor = _rig.MoonLight.color;
                _snapMoonEnabled = _rig.MoonLight.enabled;
            }

            if (_rig.SunMoonPivot != null)
                _snapPivotEuler = _rig.SunMoonPivot.localEulerAngles;

            _snapCloudLayerY = _rig.CloudLayer != null
                ? _rig.CloudLayer.transform.localPosition.y
                : float.NaN;
        }

        private void RestoreSnapshot()
        {
            RenderSettings.ambientMode = _snapAmbientMode;
            RenderSettings.sun = _snapRenderSettingsSun;
            RenderSettings.ambientSkyColor = _snapAmbientSky;
            RenderSettings.ambientEquatorColor = _snapAmbientEquator;
            RenderSettings.ambientGroundColor = _snapAmbientGround;

            if (_rig.SunLight != null)
            {
                _rig.SunLight.intensity = _snapSunIntensity;
                _rig.SunLight.color = _snapSunColor;
                _rig.SunLight.enabled = _snapSunEnabled;
            }

            if (_rig.MoonLight != null)
            {
                _rig.MoonLight.intensity = _snapMoonIntensity;
                _rig.MoonLight.color = _snapMoonColor;
                _rig.MoonLight.enabled = _snapMoonEnabled;
            }

            if (_rig.SunMoonPivot != null)
                _rig.SunMoonPivot.localEulerAngles = _snapPivotEuler;
        }

        // Тот же порядок, что в WeatherService.RotateSunMoonPivot() — формулы общие
        // (WeatherTime.ArcPivotEuler/CelestialEuler), чтобы превью и рантайм не могли разойтись.
        private void RotateSunMoonPivot(float timeOfDay01)
        {
            if (_rig.SunMoonPivot != null)
                _rig.SunMoonPivot.localEulerAngles = WeatherTime.ArcPivotEuler(_rig.ArcAzimuth, _rig.ArcTilt);

            if (_rig.SunLight != null)
            {
                _rig.SunLight.transform.localEulerAngles = WeatherTime.CelestialEuler(timeOfDay01, 0f);
                Shader.SetGlobalVector(WeatherShaderIds.SunDirectionGlobal, -_rig.SunLight.transform.forward);
            }

            if (_rig.MoonLight != null)
            {
                _rig.MoonLight.transform.localEulerAngles = WeatherTime.CelestialEuler(timeOfDay01, _rig.MoonOffsetDegrees);
                Shader.SetGlobalVector(WeatherShaderIds.MoonDirectionGlobal, -_rig.MoonLight.transform.forward);
            }

            Shader.SetGlobalFloat(WeatherShaderIds.TimeOfDay01Global, timeOfDay01);
        }

        // null, если ни одного Scene view не открыто — тогда глобал не трогаем, как и
        // WeatherService при незаполненной камере.
        private static Vector3? GetSceneViewCameraPositionWS()
        {
            SceneView view = SceneView.lastActiveSceneView;
            return view != null && view.camera != null
                ? view.camera.transform.position
                : (Vector3?)null;
        }

        // Тот же расчёт, что в WeatherService.GetSunElevation().
        private float GetSunElevation() =>
            _rig.SunLight != null ? -_rig.SunLight.transform.forward.y : 0f;

        private static void ApplyToDome(WeatherDome dome, MaterialPropertyBlock block, SkyState sky)
        {
            if (dome == null || dome.Renderer == null)
                return;

            WeatherSkyApplier.Apply(new BlockSink(block), sky);
            dome.Renderer.SetPropertyBlock(block);
        }

        // Тот же приём, что в WeatherService.ApplyAmbient() — принудительный Trilight
        // и умножение только RGB (альфа ambient-цветов Unity не читает).
        private void ApplyAmbient(SkyState sky)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;

            float m = sky.AmbientMultiplier;
            RenderSettings.ambientSkyColor = MultiplyRgb(sky.AmbientSkyColor, m);
            RenderSettings.ambientEquatorColor = MultiplyRgb(sky.AmbientEquatorColor, m);
            RenderSettings.ambientGroundColor = MultiplyRgb(sky.AmbientGroundColor, m);
        }

        private static Color MultiplyRgb(Color c, float m) =>
            new Color(c.r * m, c.g * m, c.b * m, c.a);

        private void ApplySun(SkyState sky, float sunElevation) =>
            WeatherCelestialApplier.ApplySun(
                _rig.SunLight, sky, sunElevation, _rig.HorizonFadeBand, _rig.SunShadowType);

        private void ApplyMoon(SkyState sky)
        {
            if (_rig.MoonLight == null)
                return;

            float moonElevation = -_rig.MoonLight.transform.forward.y;
            WeatherCelestialApplier.ApplyMoon(
                _rig.MoonLight, sky, moonElevation, _rig.HorizonFadeBand, _rig.MoonShadowType);

            ApplyMoonDisk(sky, moonElevation);
        }

        // Тот же приём гашения ниже горизонта, что в WeatherService.ApplyMoonDisk().
        private void ApplyMoonDisk(SkyState sky, float moonElevation)
        {
            if (_rig.Moon == null || _rig.Moon.Renderer == null)
                return;

            float visibility = SmoothStep01(-0.05f, 0.10f, moonElevation);
            Color disk = sky.MoonColor;
            disk.a *= visibility;

            _moonBlock.SetColor(WeatherShaderIds.MoonColor, disk);
            _rig.Moon.Renderer.SetPropertyBlock(_moonBlock);
        }

        private static void ClearBlock(WeatherDome dome)
        {
            if (dome != null && dome.Renderer != null)
                dome.Renderer.SetPropertyBlock(null);
        }

        private static void ClearBlock(WeatherMoon moon)
        {
            if (moon != null && moon.Renderer != null)
                moon.Renderer.SetPropertyBlock(null);
        }

        private static float SmoothStep01(float edgeHigh, float edgeLow, float value)
        {
            float t = Mathf.InverseLerp(edgeHigh, edgeLow, value);
            return t * t * (3f - 2f * t);
        }

        // Обёртка над MaterialPropertyBlock — превью-аналог MaterialSink из WeatherSkyApplier.
        private readonly struct BlockSink : IWeatherPropertySink
        {
            private readonly MaterialPropertyBlock _block;

            public BlockSink(MaterialPropertyBlock block) => _block = block;

            public void SetColor(int id, Color value) => _block.SetColor(id, value);
            public void SetFloat(int id, float value) => _block.SetFloat(id, value);
            public void SetVector(int id, Vector4 value) => _block.SetVector(id, value);
        }
    }
}
