using System;
using Infastructure.Services.CameraProvider;
using Infastructure.Services.StartGame;
using UnityEngine;
using UnityEngine.Rendering;
using WeatherSystem.Profiles;
using Zenject;

namespace WeatherSystem
{
    // Небо/солнце/окружающий свет как функция высоты камеры и времени суток. Zenject-биндинг,
    // не статичный singleton — тот же паттерн, что VolumeService вокруг Volume.
    //
    // Данные приезжают с рига (WeatherRig.Bands), а не из статических данных проекта:
    // погода — свойство сцены, а не глобальная константа игры. Полосы задают вертикальный
    // профиль уровня, пресет на каждой — состояние небесных объектов.
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
        private readonly IStartGameReceiver _startGameReceiver;
        private readonly ICameraProviderService _cameraProviderService;
        private readonly WeatherRig _rig;

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

        public float TimeOfDay01 { get; private set; }

        public WeatherService(
            IStartGameReceiver startGameReceiver,
            ICameraProviderService cameraProviderService,
            WeatherRig rig)
        {
            _startGameReceiver = startGameReceiver;
            _cameraProviderService = cameraProviderService;
            _rig = rig;

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

            // Глобалы тумана переживают выгрузку сцены, а фича тумана зарегистрирована
            // в рендерере глобально — без сброса следующая сцена без WeatherRig (меню,
            // загрузочный экран) получила бы туман предыдущего уровня.
            WeatherFogApplier.ResetGlobals();

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

            // Позиция камеры читается один раз на кадр: её потребителей теперь трое —
            // выбор полосы, альфа облачного слоя и глобалы тумана.
            Vector3? cameraPositionWS = GetCameraPositionWS();

            // Высота — от камеры, а не от паука (спек weather-presets-and-zones, решение 11):
            // погоду видит камера. Купола и плоский облачный слой и так следят за Camera.main
            // сами, расходилось только состояние — плоскость стояла у камеры, а гасилась
            // по высоте робота.
            //
            // Пока камеры нет (до BuildLevelState), берём низ мира — начало первой полосы.
            // Прежний фолбэк опирался на пороги высоты рига, но они удалены: при явных
            // границах полос это была вторая, несогласованная разметка высоты.
            float worldY = cameraPositionWS.HasValue
                ? cameraPositionWS.Value.y
                : BottomOfWorld(_rig.Bands);

            // Порядок важен: сперва поворачиваем дугу, и только потом читаем с неё
            // фактическую высоту светил — иначе замер отстаёт от поворота на кадр.
            RotateSunMoonPivot();

            float sunElevation = GetSunElevation();

            // Один расчёт SkyState на кадр — небо, плоский облачный слой, ambient,
            // солнце и луна читают из него, а не пересчитывают блендинг каждый по-своему.
            // Свёртка складывает обе оси: полосы рига по высоте и зоны влияния по месту.
            // Пустой список полос она переваривает штатно и отдаёт default(SkyState),
            // тот же путь, что и полоса без пресета (см. комментарий в WeatherFogApplier).
            SkyState sky = WeatherComposer.Compose(
                _rig.Bands, worldY, TimeOfDay01, cameraPositionWS, Time.deltaTime);

            ApplySky(sky);
            ApplyAmbient(sky);
            ApplySun(sky, sunElevation);
            ApplyMoon(sky);
            ApplyCloudLayer(sky, cameraPositionWS);
            WeatherFogApplier.ApplyGlobals(sky, cameraPositionWS);
        }

        // Низ мира — начало первой полосы. Полосы отсортированы по возрастанию StartY
        // (контракт SkyBand), поэтому первая и есть самая нижняя.
        private static float BottomOfWorld(SkyBand[] bands) =>
            bands != null && bands.Length > 0 ? bands[0].StartY : 0f;

        // null, пока камера не назначена: ICameraProviderService заполняется в BuildLevelState,
        // и до этого момента честнее не трогать глобал вовсе, чем выдать нуль за настоящую
        // позицию — на нуле туман на воде считался бы от начала координат.
        private Vector3? GetCameraPositionWS() =>
            _cameraProviderService.Camera != null
                ? _cameraProviderService.CameraTransform.position
                : (Vector3?)null;

        // Высота камеры, а не паука: сам CloudLayer уже подтягивает свою XZ-позицию и размер
        // под Camera.main, и гасить его по чужой высоте означало бы, что плоскость стоит
        // у камеры, но исчезает по роботу. Смысл фейда — не показать плоскость с ребра,
        // а с ребра её видит именно камера.
        private void ApplyCloudLayer(SkyState sky, Vector3? cameraPositionWS)
        {
            if (_cloudMaterialInstance == null || _rig.CloudLayer == null || !cameraPositionWS.HasValue)
                return;

            float distance = Mathf.Abs(cameraPositionWS.Value.y - _rig.CloudLayer.BandCenterY);
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

            // Для вращения звёздной сферы (SHD_Weather_DomeSky) — сутки как угол поворота.
            // См. .scratch/sky-night-and-star-dome/spec.md, решение #10.
            Shader.SetGlobalFloat(WeatherShaderIds.TimeOfDay01Global, TimeOfDay01);
        }

        // Один вызов бьёт по всем трём материалам (старый skybox + оба купола) —
        // Material.SetFloat/SetColor на свойстве, которого нет в конкретном шейдере,
        // молча ничего не делает (задокументированное поведение Unity, не ошибка).
        // Поэтому не нужно разбирать, что из ~35 свойств относится к Sky, а что к Clouds:
        // каждый материал сам берёт то, что у него объявлено в CBUFFER, и игнорирует остальное.
        private void ApplySky(SkyState sky)
        {
            ApplySkyToMaterial(RenderSettings.skybox, sky);
            ApplySkyToMaterial(_skyDomeMaterialInstance, sky);
            ApplySkyToMaterial(_cloudsDomeMaterialInstance, sky);
        }

        // Список свойств живёт в WeatherSkyApplier — общий с Edit Mode превью
        // (Assets/Editor/Weather/WeatherPreviewDriver.cs), чтобы оба пути не могли
        // разойтись между собой при добавлении нового свойства шейдера.
        private static void ApplySkyToMaterial(Material mat, SkyState sky)
        {
            if (mat == null)
                return;

            WeatherSkyApplier.Apply(new MaterialSink(mat), sky);
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
