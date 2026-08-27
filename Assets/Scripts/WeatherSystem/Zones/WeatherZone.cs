using UnityEngine;
using WeatherSystem.Profiles;

namespace WeatherSystem
{
    // Зона влияния погоды — объём, внутри которого звучит свой пресет. Вторая ось системы:
    // риг задаёт погоду по высоте, зона — по месту. Спек .scratch/weather-presets-and-zones,
    // решения 3, 4, 12, 13.
    //
    // ВАЖНО: зона НЕ слушает OnTriggerEnter/Exit. Соблазн велик — так устроена соседняя
    // SpatialFXZone через ObserverTrigger, — но события физики в Edit Mode не приходят вовсе,
    // а зона обязана работать и там. Поэтому вес считается геометрически, запросом ближайшей
    // точки коллайдера, ровно как у CozyBiome. Один путь на оба режима, а не два расходящихся:
    // расхождение Edit/Play в этой системе уже дважды стоило дорого (тикеты 01 и 02).
    //
    // Позицию наблюдателя зона не спрашивает сама — её передают. В игре это камера
    // из ICameraProviderService, в редакторе камера Scene view (решение 11), и знание об этом
    // должно жить в одном месте у вызывающего, а не размножаться по компонентам.
    //
    // Высота внутри зоны не работает: зона несёт одно состояние неба и на полном весе делает
    // его одинаковым на всех высотах своего объёма. Это решение 1a, а не недоделка —
    // высотная вариация задаётся ТОЛЬКО полосами рига. Стопка боксов друг над другом
    // заменой высоте не является и предлагаться не должна.
    [ExecuteAlways]
    [RequireComponent(typeof(Collider))]
    public class WeatherZone : MonoBehaviour
    {
        public enum TransitionMode
        {
            // Вес растёт по мере приближения: непрерывная функция расстояния, поэтому
            // плавность гарантирована самой формулой, а не сглаживанием.
            Distance,

            // Вес набирается и спадает за заданное время после пересечения границы.
            // Внутри/снаружи определяется геометрически, без событий физики.
            Time
        }

        [Tooltip("Вес зоны при полном влиянии. 1 — пресет зоны полностью перекрывает погоду рига.")]
        [SerializeField, Range(0f, 1f)] private float _maxWeight = 1f;

        [Tooltip("Кто кладётся поверх при перекрытии зон. Упорядочивание — тикет 07.")]
        [SerializeField] private int _priority;

        [SerializeField] private TransitionMode _transition = TransitionMode.Distance;

        [Tooltip("Distance: на этом расстоянии до объёма вес равен нулю, вплотную — максимуму.")]
        [SerializeField] private float _transitionDistance = 10f;

        [Tooltip("Time: за столько секунд вес набирается и спадает после пересечения границы.")]
        [SerializeField] private float _transitionSeconds = 2f;

        private Collider _collider;
        private WeatherZonePreset _presetModule;
        private float _weight;
        private bool _warnedUnsupportedCollider;

        public int Priority => _priority;
        public float Weight => _weight;

        // Данные погоды живут в отдельном модуле на том же объекте (решение 4), не здесь.
        // Смысл не в чистоте: FX — пыль, снег, дождь — добавятся третьим компонентом,
        // не ломая расставленные зоны и не заводя второй тип зоны со своей геометрией.
        public WeatherPreset Preset
        {
            get
            {
                if (_presetModule == null)
                    _presetModule = GetComponent<WeatherZonePreset>();

                return _presetModule != null ? _presetModule.Preset : null;
            }
        }

        private void OnEnable() => WeatherZoneRegistry.Register(this);

        private void OnDisable()
        {
            WeatherZoneRegistry.Unregister(this);
            _weight = 0f;
        }

        // deltaTime параметром, а не Time.deltaTime внутри: в Edit Mode его не существует,
        // и временнóй режим стоял бы намертво. Вызывающий подставляет свой источник.
        public float UpdateWeight(Vector3 observerPositionWS, float deltaTime)
        {
            if (!TryResolveCollider())
            {
                _weight = 0f;
                return 0f;
            }

            Vector3 closest = _collider.ClosestPoint(observerPositionWS);

            if (_transition == TransitionMode.Distance)
            {
                float distance = Vector3.Distance(observerPositionWS, closest);
                float span = Mathf.Max(_transitionDistance, 0.0001f);
                _weight = _maxWeight * Mathf.Clamp01(1f - distance / span);
                return _weight;
            }

            // Внутри объёма ближайшая точка совпадает с самой позицией. Сравниваем квадрат
            // расстояния с допуском, а не векторы напрямую: точного равенства у float не бывает.
            bool inside = (closest - observerPositionWS).sqrMagnitude < 1e-6f;
            float target = inside ? _maxWeight : 0f;

            if (_transitionSeconds <= 0f)
            {
                _weight = target;
                return _weight;
            }

            _weight = Mathf.MoveTowards(_weight, target, deltaTime / _transitionSeconds);
            return _weight;
        }

        // ClosestPoint работает только на выпуклых коллайдерах. На невыпуклом MeshCollider
        // Unity возвращает мусор, и зона молча вела бы себя непредсказуемо — поэтому говорим
        // об этом автору уровня один раз и вслух, а не сыпем в консоль каждый кадр.
        private bool TryResolveCollider()
        {
            if (_collider == null)
                _collider = GetComponent<Collider>();

            if (_collider == null)
                return false;

            var mesh = _collider as MeshCollider;
            if (mesh != null && !mesh.convex)
            {
                if (!_warnedUnsupportedCollider)
                {
                    Debug.LogWarning(
                        $"[WeatherZone] '{name}': невыпуклый MeshCollider не поддерживается — " +
                        "вес зоны считается по ближайшей точке объёма. Включи Convex " +
                        "или поставь Box/Sphere/Capsule.", this);
                    _warnedUnsupportedCollider = true;
                }

                return false;
            }

            return true;
        }

        private void OnDrawGizmosSelected()
        {
            if (!TryResolveCollider() || _transition != TransitionMode.Distance)
                return;

            // Показываем, откуда начинается переход: без этого подобрать дистанцию можно
            // только вслепую, а плавность входа — главное требование к зоне.
            Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.25f);
            Bounds bounds = _collider.bounds;
            Gizmos.DrawWireCube(bounds.center, bounds.size + Vector3.one * (_transitionDistance * 2f));
        }
    }
}
