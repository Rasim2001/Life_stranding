using Infastructure.StaticData.WeatherSystem;
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
        private const float TwilightWidth = 0.05f;

        private readonly MaterialPropertyBlock _skyBlock = new MaterialPropertyBlock();
        private readonly MaterialPropertyBlock _cloudsBlock = new MaterialPropertyBlock();
        private readonly MaterialPropertyBlock _moonBlock = new MaterialPropertyBlock();

        private WeatherRig _rig;
        private bool _running;

        private AmbientMode _snapAmbientMode;
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

            RestoreSnapshot();

            ClearBlock(_rig.SkyDome);
            ClearBlock(_rig.CloudsDome);
            ClearBlock(_rig.Moon);

            _running = false;
            _rig = null;
        }

        public void Apply(WeatherStaticData staticData, float worldY, float timeOfDay01)
        {
            if (!_running || _rig == null || staticData == null)
                return;

            RotateSunMoonPivot(timeOfDay01);

            float sunElevation = GetSunElevation();
            float nightFactor = SmoothStep01(TwilightWidth, -TwilightWidth, sunElevation);

            SkyState sky = SkyBandBlender.Evaluate(staticData.Bands, worldY, timeOfDay01);

            ApplyToDome(_rig.SkyDome, _skyBlock, sky, nightFactor);
            ApplyToDome(_rig.CloudsDome, _cloudsBlock, sky, nightFactor);
            ApplyAmbient(sky);
            ApplySun(sky, sunElevation);
            ApplyMoon(sky);
        }

        private void TakeSnapshot()
        {
            _snapAmbientMode = RenderSettings.ambientMode;
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
        }

        private void RestoreSnapshot()
        {
            RenderSettings.ambientMode = _snapAmbientMode;
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

        // Тот же порядок, что в WeatherService.RotateSunMoonPivot() — формула пивота
        // общая (WeatherTime.PivotEuler), чтобы превью и рантайм не могли разойтись.
        private void RotateSunMoonPivot(float timeOfDay01)
        {
            if (_rig.SunMoonPivot != null)
                _rig.SunMoonPivot.localEulerAngles = WeatherTime.PivotEuler(timeOfDay01);

            if (_rig.SunLight != null)
                Shader.SetGlobalVector(WeatherShaderIds.SunDirectionGlobal, -_rig.SunLight.transform.forward);

            if (_rig.MoonLight != null)
                Shader.SetGlobalVector(WeatherShaderIds.MoonDirectionGlobal, -_rig.MoonLight.transform.forward);
        }

        // Тот же расчёт, что в WeatherService.GetSunElevation().
        private float GetSunElevation() =>
            _rig.SunLight != null ? -_rig.SunLight.transform.forward.y : 0f;

        private static void ApplyToDome(WeatherDome dome, MaterialPropertyBlock block, SkyState sky, float nightFactor)
        {
            if (dome == null || dome.Renderer == null)
                return;

            WeatherSkyApplier.Apply(new BlockSink(block), sky, nightFactor);
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

        private void ApplySun(SkyState sky, float sunElevation)
        {
            if (_rig.SunLight == null)
                return;

            _rig.SunLight.enabled = sunElevation > 0f;
            if (!_rig.SunLight.enabled)
                return;

            _rig.SunLight.intensity = sky.SunIntensity;
            _rig.SunLight.color = sky.SunColor;
        }

        private void ApplyMoon(SkyState sky)
        {
            if (_rig.MoonLight == null)
                return;

            float moonElevation = -_rig.MoonLight.transform.forward.y;
            _rig.MoonLight.enabled = moonElevation > 0f;

            if (_rig.MoonLight.enabled)
            {
                _rig.MoonLight.intensity = sky.MoonIntensity;
                _rig.MoonLight.color = sky.MoonColor;
            }

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
