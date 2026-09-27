using UnityEngine;
using WeatherSystem.Profiles;

namespace WeatherSystem
{
    // Данные и ссылки сцены для WeatherService — та же роль, что у Volume для VolumeService.
    // Настраивается на конкретном экземпляре в сцене: у каждого уровня своя геометрия.
    //
    // Солнце и луна — дети одного пивота, разведённые на 180°: один разворот пивота двигает
    // обе дуги разом, без отдельной орбитальной механики.
    public class WeatherRig : MonoBehaviour
    {
        // Вертикальный профиль погоды этого уровня: сколько полос, где границы, насколько резок
        // переход и какой пресет звучит на каждой. Это и есть глобальная погода сцены —
        // отдельной сущности «глобальный пресет» нет (спек, решения 5 и 6).
        //
        // Полосы свои у каждой сцены: у каждого уровня своя геометрия, и правка в одной сцене
        // не должна трогать другую. Массив на префабе — дефолт для сцен, которые его не
        // переопределяли; рассчитывать на доставку ПОЗДНИХ правок префаба в уже настроенные
        // сцены нельзя (решение 6a), но стартовое значение он даёт, и без него песочницы
        // получили бы пустой список и чёрное небо.
        //
        // Массив обязан быть отсортирован по возрастанию StartY — контракт в SkyBand.cs.
        [Header("Weather — altitude bands")]
        [SerializeField] private SkyBand[] _bands;

        [SerializeField] private Light _sunLight;
        [SerializeField] private Light _moonLight;
        [SerializeField] private Transform _sunMoonPivot;
        [SerializeField] private CloudLayer _cloudLayer;

        // Опционально: сцены на старом RenderSettings.skybox (SHD_Weather_Sky /
        // SHD_Weather_SkyProcedural) не обязаны их иметь — WeatherService проверяет
        // на null и пропускает купольную ветку, если рига в сцене без них.
        [Header("Sky domes (optional — mesh-based sky, see docs/plan)")]
        [SerializeField] private WeatherDome _skyDome;
        [SerializeField] private WeatherDome _cloudsDome;
        [SerializeField] private WeatherMoon _moon;

        // 0 — полночь, 0.25 — восход, 0.5 — полдень, 0.75 — закат (как у Cozy —
        // MeridiemTime.cs, буквально часы/24; см. WeatherTime.cs, почему мигрировали
        // со старой шкалы, где 0 было восходом). День занимает 0.25..0.75.
        // Дефолт 0.44 ≈ позднее утро: то же освещение (10:30), которое до замены
        // выставлял Cozy. Ставить 0.75 нельзя — это ровно закат, солнце уже выключено.
        [Header("Time of day")]
        [SerializeField, Range(0f, 1f)] private float _startingTimeOfDay01 = 0.44f;
        [SerializeField] private bool _timeOfDayRunning;
        [Tooltip("Длина игровых суток в секундах реального времени. Для проверки дуги в песочнице ставится маленькой.")]
        [SerializeField] private float _dayLengthSeconds = 600f;

        // Пивот несёт статическую ориентацию дуги (где горизонт), лампы едут по ней от времени —
        // раскладка Cozy (CozyWeather.cs:908-909), см. WeatherTime.ArcPivotEuler/CelestialEuler.
        // Дефолты Azimuth=-30, Tilt=30 воспроизводят точку восхода (150°) и высоту (60°) дуги,
        // авторенные до введения этих ручек.
        [Header("Небесная механика")]
        [Tooltip("Азимут восхода = 180 + Azimuth.")]
        [SerializeField, Range(-180f, 180f)] private float _arcAzimuth = -30f;
        [Tooltip("Макс. высота дуги = 90 - Tilt.")]
        [SerializeField, Range(0f, 80f)] private float _arcTilt = 30f;
        [Tooltip("Отставание луны по той же траектории, что и солнце.")]
        [SerializeField, Range(90f, 180f)] private float _moonOffsetDegrees = 180f;
        [Tooltip("Полуширина полосы фейда солнце↔луна по высоте = длина сумерек.")]
        [SerializeField, Range(0f, 0.3f)] private float _horizonFadeBand = 0.07f;
        [SerializeField] private LightShadows _sunShadowType = LightShadows.Soft;
        [SerializeField] private LightShadows _moonShadowType = LightShadows.Soft;

        public SkyBand[] Bands => _bands;
        public Light SunLight => _sunLight;
        public Light MoonLight => _moonLight;
        public Transform SunMoonPivot => _sunMoonPivot;
        public CloudLayer CloudLayer => _cloudLayer;
        public WeatherDome SkyDome => _skyDome;
        public WeatherDome CloudsDome => _cloudsDome;
        public WeatherMoon Moon => _moon;
        public float StartingTimeOfDay01 => _startingTimeOfDay01;
        public bool TimeOfDayRunning => _timeOfDayRunning;
        public float DayLengthSeconds => _dayLengthSeconds;

        public float ArcAzimuth => _arcAzimuth;
        public float ArcTilt => _arcTilt;
        public float MoonOffsetDegrees => _moonOffsetDegrees;
        public float HorizonFadeBand => _horizonFadeBand;
        public LightShadows SunShadowType => _sunShadowType;
        public LightShadows MoonShadowType => _moonShadowType;

        // Инвалидирует кэш Edit Mode превью на любую правку сериализованных полей рига —
        // и через панель Weather Control, и через дефолтный инспектор напрямую. Оба пути
        // идут через один и тот же слой сериализации/undo, поэтому один хук в риге
        // закрывает оба сразу, без списка "интересных" полей в WeatherEditorDriver.
        // Эмпирически подтверждено (план, срез 4, п.1): OnValidate срабатывает и на
        // SerializedObject.ApplyModifiedProperties(), не только на правку руками в инспекторе.
        //
        // Вызов через рефлексию, а не прямая ссылка на тип: WeatherRig.cs компилируется
        // в рантайм-сборку (Assembly-CSharp), WeatherEditorDriver — в Editor-сборку
        // (Assembly-CSharp-Editor); рантайм-сборка не может ссылаться на Editor-сборку
        // напрямую даже под #if UNITY_EDITOR — это ограничение порядка компиляции, не
        // видимости кода, компилятор отказывает с CS0103.
#if UNITY_EDITOR
        private static System.Reflection.MethodInfo _invalidateMethod;

        private void OnValidate()
        {
            if (_invalidateMethod == null)
            {
                System.Type driverType = System.Type.GetType(
                    "SpiderRig.Editor.Weather.WeatherEditorDriver, Assembly-CSharp-Editor");
                _invalidateMethod = driverType?.GetMethod(
                    "Invalidate", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            }

            _invalidateMethod?.Invoke(null, null);
        }
#endif
    }
}
