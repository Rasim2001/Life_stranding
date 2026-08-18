using System;
using Infastructure.Services.Registries.SpiderRegistry;
using Infastructure.Services.StartGame;
using Infastructure.StaticData.StaticDataService;
using Infastructure.StaticData.WeatherSystem;
using UnityEngine;
using UnityEngine.Rendering;
using WeatherSystem.Profiles;
using Zenject;

namespace WeatherSystem
{
    // Небо/солнце/окружающий свет как функция высоты паука и времени суток. Zenject-биндинг,
    // не статичный singleton — тот же паттерн, что VolumeService вокруг Volume.
    //
    // Дуга солнца/луны — поворот одного пивота, формула в WeatherTime.PivotEuler (общая
    // с Edit Mode превью). Солнце и луна — его дети, разведённые на 180°, поэтому одна
    // ротация двигает обе дуги и они всегда на противоположных сторонах неба. Высота
    // солнца над горизонтом (elevation) берётся не из TimeOfDay01, а прямо из transform —
    // без риска рассинхронизации с тем, что фактически повернуло пивот.
    //
    // Шкала TimeOfDay01: 0 полночь · 0.25 восход · 0.5 полдень · 0.75 закат
    // (см. WeatherTime.cs — почему именно так, а не 0=восход, как было раньше).
    public class WeatherService : IWeatherService, IInitializable, ITickable, IDisposable
    {
        private const float TwilightWidth = 0.05f;

        private readonly ISpiderRegistryService _spiderRegistryService;
        private readonly IStartGameReceiver _startGameReceiver;
        private readonly WeatherRig _rig;
        private readonly WeatherStaticData _staticData;

        private Material _originalSkybox;
        private Material _skyboxInstance;

        private Material _originalCloudMaterial;
        private Material _cloudMaterialInstance;

        private Material _originalSkyDomeMaterial;
        private Material _skyDomeMaterialInstance;

        private Material _originalCloudsDomeMaterial;
        private Material _cloudsDomeMaterialInstance;

        private Material _originalMoonMaterial;
        private Material _moonMaterialInstance;

        public float NormalizedAltitude { get; private set; }
        public float TimeOfDay01 { get; private set; }

        public WeatherService(
            ISpiderRegistryService spiderRegistryService,
            IStartGameReceiver startGameReceiver,
            WeatherRig rig,
            IStaticDataService staticDataService)
        {
            _spiderRegistryService = spiderRegistryService;
            _startGameReceiver = startGameReceiver;
            _rig = rig;
            _staticData = staticDataService.WeatherStaticData;

            TimeOfDay01 = _rig.StartingTimeOfDay01;
        }

        public void Initialize()
        {
            _startGameReceiver.OnStartGameHappened += ResetToStartingTime;

            // Работаем по рантайм-копии скайбокс-материала, а не по самому ассету.
            // RenderSettings.skybox в редакторе указывает на общий ассет, и запись в него
            // каждый кадр помечает его грязным: авторские значения молча затираются тем,
            // где в тот момент был паук, и в git приезжает паразитный дифф материала.
            _originalSkybox = RenderSettings.skybox;

            if (_originalSkybox != null)
            {
                _skyboxInstance = new Material(_originalSkybox);
                RenderSettings.skybox = _skyboxInstance;
            }

            // Тот же приём для материала облачного слоя: renderer.sharedMaterial — тоже
            // общий ассет, а не рантайм-копия сама по себе.
            if (_rig.CloudLayer != null)
            {
                _originalCloudMaterial = _rig.CloudLayer.Renderer.sharedMaterial;
                if (_originalCloudMaterial != null)
                {
                    _cloudMaterialInstance = new Material(_originalCloudMaterial);
                    _rig.CloudLayer.Renderer.sharedMaterial = _cloudMaterialInstance;
                }
            }

            // Купола — тот же приём, опционально (WeatherRig.SkyDome/CloudsDome
            // не обязаны быть назначены в сценах на старом RenderSettings.skybox).
            if (_rig.SkyDome != null)
            {
                _originalSkyDomeMaterial = _rig.SkyDome.Renderer.sharedMaterial;
                if (_originalSkyDomeMaterial != null)
                {
                    _skyDomeMaterialInstance = new Material(_originalSkyDomeMaterial);
                    _rig.SkyDome.Renderer.sharedMaterial = _skyDomeMaterialInstance;
                }
            }

            if (_rig.CloudsDome != null)
            {
                _originalCloudsDomeMaterial = _rig.CloudsDome.Renderer.sharedMaterial;
                if (_originalCloudsDomeMaterial != null)
                {
                    _cloudsDomeMaterialInstance = new Material(_originalCloudsDomeMaterial);
                    _rig.CloudsDome.Renderer.sharedMaterial = _cloudsDomeMaterialInstance;
                }
            }

            if (_rig.Moon != null)
            {
                _originalMoonMaterial = _rig.Moon.Renderer.sharedMaterial;
                if (_originalMoonMaterial != null)
                {
                    _moonMaterialInstance = new Material(_originalMoonMaterial);
                    _rig.Moon.Renderer.sharedMaterial = _moonMaterialInstance;
                }
            }
        }

        public void Dispose()
        {
            _startGameReceiver.OnStartGameHappened -= ResetToStartingTime;

            if (_skyboxInstance != null)
            {
                RenderSettings.skybox = _originalSkybox;
                UnityEngine.Object.Destroy(_skyboxInstance);
                _skyboxInstance = null;
            }

            if (_cloudMaterialInstance != null)
            {
                _rig.CloudLayer.Renderer.sharedMaterial = _originalCloudMaterial;
                UnityEngine.Object.Destroy(_cloudMaterialInstance);
                _cloudMaterialInstance = null;
            }

            if (_skyDomeMaterialInstance != null)
            {
                _rig.SkyDome.Renderer.sharedMaterial = _originalSkyDomeMaterial;
                UnityEngine.Object.Destroy(_skyDomeMaterialInstance);
                _skyDomeMaterialInstance = null;
            }

            if (_cloudsDomeMaterialInstance != null)
            {
                _rig.CloudsDome.Renderer.sharedMaterial = _originalCloudsDomeMaterial;
                UnityEngine.Object.Destroy(_cloudsDomeMaterialInstance);
                _cloudsDomeMaterialInstance = null;
            }

            if (_moonMaterialInstance != null)
            {
                _rig.Moon.Renderer.sharedMaterial = _originalMoonMaterial;
                UnityEngine.Object.Destroy(_moonMaterialInstance);
                _moonMaterialInstance = null;
            }
        }

        private void ResetToStartingTime() =>
            TimeOfDay01 = _rig.StartingTimeOfDay01;

        public void Tick()
        {
            if (_rig.TimeOfDayRunning)
                TimeOfDay01 = Mathf.Repeat(TimeOfDay01 + Time.deltaTime / _rig.DayLengthSeconds, 1f);

            // До первого спавна паука полосы неба резолвятся от нижней границы рига —
            // тот же фолбэк, что и раньше был неявно в NormalizedAltitude = 0.
            float worldY = _rig.AltitudeMinY;
            if (_spiderRegistryService.Spider != null)
            {
                worldY = _spiderRegistryService.Spider.transform.position.y;
                NormalizedAltitude = Mathf.InverseLerp(_rig.AltitudeMinY, _rig.AltitudeMaxY, worldY);
            }

            // Порядок важен: сперва поворачиваем дугу, и только потом читаем с неё
            // фактическую высоту светил — иначе замер отстаёт от поворота на кадр.
            RotateSunMoonPivot();

            float sunElevation = GetSunElevation();
            float nightFactor = SmoothStep01(TwilightWidth, -TwilightWidth, sunElevation);

            // Один расчёт SkyState на кадр — небо, плоский облачный слой, ambient,
            // солнце и луна читают из него, а не пересчитывают блендинг каждый по-своему.
            SkyState sky = SkyBandBlender.Evaluate(_staticData.Bands, worldY, TimeOfDay01);

            ApplySky(sky, nightFactor);
            ApplyAmbient(sky);
            ApplySun(sky, sunElevation);
            ApplyMoon(sky);
            ApplyCloudLayer(sky);
        }

        private void ApplyCloudLayer(SkyState sky)
        {
            if (_cloudMaterialInstance == null || _rig.CloudLayer == null || _spiderRegistryService.Spider == null)
                return;

            float spiderY = _spiderRegistryService.Spider.transform.position.y;
            float distance = Mathf.Abs(spiderY - _rig.CloudLayer.BandCenterY);
            float alpha = 1f - Mathf.Clamp01(distance / _rig.CloudLayer.FadeDistance);
            _cloudMaterialInstance.SetFloat(WeatherShaderIds.Alpha, alpha);

            // Тот же цвет, что у купольных облаков — иначе плоский слой и небо разъедутся
            // по палитре при первой же смене профиля.
            _cloudMaterialInstance.SetColor(WeatherShaderIds.CloudColor, sky.CloudColor);
        }

        // Высота солнца берётся из фактического направления света, а не из sin(2*PI*t).
        // Формула верна только когда у солнца в риге нет собственного наклона; стоит его
        // наклонить — и она расходится с тем, что видно в небе: свет гаснет при солнце
        // над горизонтом и горит, когда оно уже зашло. Замер по трансформу верен всегда.
        private float GetSunElevation() =>
            _rig.SunLight != null
                ? -_rig.SunLight.transform.forward.y
                : Mathf.Sin(TimeOfDay01 * 2f * Mathf.PI);

        private void RotateSunMoonPivot()
        {
            if (_rig.SunMoonPivot != null)
                _rig.SunMoonPivot.localEulerAngles = WeatherTime.PivotEuler(TimeOfDay01);

            // Глобал, не свойство материала: направление на солнце понадобится не только
            // небу, позже туману и воде. -forward, а не forward: свет "смотрит" от солнца
            // к земле, а нам нужно направление К солнцу — тот же приём, что в GetSunElevation().
            if (_rig.SunLight != null)
                Shader.SetGlobalVector(WeatherShaderIds.SunDirectionGlobal, -_rig.SunLight.transform.forward);

            if (_rig.MoonLight != null)
                Shader.SetGlobalVector(WeatherShaderIds.MoonDirectionGlobal, -_rig.MoonLight.transform.forward);
        }

        // Один вызов бьёт по всем трём материалам (старый skybox + оба купола) —
        // Material.SetFloat/SetColor на свойстве, которого нет в конкретном шейдере,
        // молча ничего не делает (задокументированное поведение Unity, не ошибка).
        // Поэтому не нужно разбирать, что из ~35 свойств относится к Sky, а что к Clouds:
        // каждый материал сам берёт то, что у него объявлено в CBUFFER, и игнорирует остальное.
        private void ApplySky(SkyState sky, float nightFactor)
        {
            ApplySkyToMaterial(RenderSettings.skybox, sky, nightFactor);
            ApplySkyToMaterial(_skyDomeMaterialInstance, sky, nightFactor);
            ApplySkyToMaterial(_cloudsDomeMaterialInstance, sky, nightFactor);
        }

        // Список свойств живёт в WeatherSkyApplier — общий с Edit Mode превью
        // (Assets/Editor/Weather/WeatherPreviewDriver.cs), чтобы оба пути не могли
        // разойтись между собой при добавлении нового свойства шейдера.
        private static void ApplySkyToMaterial(Material mat, SkyState sky, float nightFactor)
        {
            if (mat == null)
                return;

            WeatherSkyApplier.Apply(new MaterialSink(mat), sky, nightFactor);
        }

        // ambientMode принудительно, не полагаясь на разметку сцены — сцены на легаси
        // RenderSettings.skybox молча оставляли AmbientMode.Skybox, и Unity в этом режиме
        // ambientSkyColor/Equator/Ground игнорирует целиком: сворачивает ambient из кубмапы
        // скайбокса, а наше небо теперь меш-купол, не скайбокс. Три цвета писались в никуда.
        // Тот же приём у Cozy — CozyWeather.cs: RenderSettings.ambientMode = Trilight каждый кадр.
        private void ApplyAmbient(SkyState sky)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;

            // Множитель — только RGB, не альфа: альфа ambient-цветов Unity не читает,
            // а умножение Color*float тянет альфу вместе с цветом (ровно так у Cozy
            // multiplier=1.5 даёт alpha=1.5 в инспекторе — не решение, побочный эффект).
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

            // Строго исключаем солнце, когда оно физически под горизонтом — а не только гасим яркость.
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

            // Читаем высоту луны с её собственного трансформа, а не как -sunElevation:
            // так связка остаётся верной, даже если луну в риге перевесят иначе.
            float moonElevation = -_rig.MoonLight.transform.forward.y;
            _rig.MoonLight.enabled = moonElevation > 0f;

            if (_rig.MoonLight.enabled)
            {
                _rig.MoonLight.intensity = sky.MoonIntensity;
                _rig.MoonLight.color = sky.MoonColor;
            }

            ApplyMoonDisk(sky, moonElevation);
        }

        // Диск луны идёт с ZWrite Off и сортируется очередью, не глубиной — без явного
        // гашения ниже горизонта он рисовался бы поверх купола неба везде, где в кадре
        // нет непрозрачной геометрии уровня. Плавный, а не мгновенный, порог — тот же
        // приём twilight-сглаживания, что и у nightFactor в Tick().
        private void ApplyMoonDisk(SkyState sky, float moonElevation)
        {
            if (_moonMaterialInstance == null)
                return;

            float visibility = SmoothStep01(-0.05f, 0.10f, moonElevation);
            Color disk = sky.MoonColor;
            disk.a *= visibility;
            _moonMaterialInstance.SetColor(WeatherShaderIds.MoonColor, disk);
        }

        private static float SmoothStep01(float edgeHigh, float edgeLow, float value)
        {
            float t = Mathf.InverseLerp(edgeHigh, edgeLow, value);
            return t * t * (3f - 2f * t);
        }
    }
}
