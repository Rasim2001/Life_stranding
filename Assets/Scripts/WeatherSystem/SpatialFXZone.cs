using Common;
using UnityEngine;

namespace WeatherSystem
{
    // Генерик-зона: паук входит — вокруг обзора нарастает туман и включаются частицы,
    // выходит — спадают. Не сделана под облака конкретно: та же механика годится под
    // будущий туман в пещере, брызги у воды, ветер — новой зоне нужен только другой
    // коллайдер и другие значения полей, кода не нужно.
    //
    // Пост-обработка — встроенный URP Local Volume: этот же GameObject несёт Collider
    // (границы) и Volume с isGlobal=false (блендится нативно, без единой строчки кода
    // на весовой расчёт).
    //
    // Туман — отдельно, через RenderSettings.fog: в URP, в отличие от HDRP, тумана
    // в системе Volume нет, это проверено. Вес зоны для тумана считается по времени
    // входа/выхода (Update + MoveTowards), а не по расстоянию до коллайдера — расстояние
    // потребовало бы опроса каждой зоны каждый кадр, ровно антипаттерн CozyBiome, от
    // которого эта система сознательно уходит. Следствие: туман глобальный, один
    // смешанный на кадр — зоны тумана не должны перекрываться друг с другом.
    //
    // Частицы — на CameraFXHost, не здесь: см. комментарий в CameraFXHost.cs.
    [RequireComponent(typeof(Collider))]
    public class SpatialFXZone : MonoBehaviour
    {
        [SerializeField] private ObserverTrigger _observerTrigger;

        [Header("Fog — RenderSettings.fog, зоны тумана не перекрываются")]
        [SerializeField] private Color _fogColor = new Color(0.7f, 0.75f, 0.8f);
        [SerializeField] private float _fogDensity = 0.05f;
        [SerializeField] private float _fadeInSeconds = 1.5f;
        [SerializeField] private float _fadeOutSeconds = 1.5f;

        [Header("Частицы на CameraFXHost — включаются/выключаются, не создаются здесь")]
        [SerializeField] private ParticleSystem[] _cameraParticles;

        private float _targetWeight;
        private float _currentWeight;

        private bool _hasCapturedOriginalFog;
        private bool _originalFogEnabled;
        private Color _originalFogColor;
        private float _originalFogDensity;

        private void OnEnable()
        {
            if (_observerTrigger == null)
            {
                Debug.LogError($"{nameof(SpatialFXZone)} на '{name}': не назначен {nameof(ObserverTrigger)} — зона работать не будет.", this);
                return;
            }

            _observerTrigger.OnTriggerEnterHappened += HandleEnter;
            _observerTrigger.OnTriggerExitHappened += HandleExit;
        }

        private void OnDisable()
        {
            if (_observerTrigger != null)
            {
                _observerTrigger.OnTriggerEnterHappened -= HandleEnter;
                _observerTrigger.OnTriggerExitHappened -= HandleExit;
            }

            // Если зону выключили или уничтожили, пока паук внутри, Update() больше
            // не выполнится — и туман остался бы применённым навсегда: на весь уровень,
            // без единого объекта, способного его снять. Достижимо буднично: дизайнер
            // выключил объект, зону уничтожил геймплей, выгрузился чанк при стриминге.
            // Поэтому снимаем состояние здесь, а не полагаемся на выход из зоны.
            _currentWeight = 0f;
            _targetWeight = 0f;

            if (_hasCapturedOriginalFog)
                RestoreOriginalFog();

            foreach (ParticleSystem ps in _cameraParticles)
                if (ps != null)
                    ps.Stop();
        }

        private void HandleEnter(Collider spiderCollider)
        {
            CaptureOriginalFogIfNeeded();
            _targetWeight = 1f;

            foreach (ParticleSystem ps in _cameraParticles)
                if (ps != null)
                    ps.Play();
        }

        private void HandleExit(Collider spiderCollider)
        {
            _targetWeight = 0f;

            foreach (ParticleSystem ps in _cameraParticles)
                if (ps != null)
                    ps.Stop();
        }

        private void Update()
        {
            // В покое (никогда не входили или уже полностью вышли и остыли) ничего
            // не трогаем — не переопределяем туман, который мог выставить кто-то другой.
            if (_currentWeight == 0f && _targetWeight == 0f)
                return;

            float fadeSeconds = _currentWeight < _targetWeight ? _fadeInSeconds : _fadeOutSeconds;
            _currentWeight = Mathf.MoveTowards(_currentWeight, _targetWeight, Time.deltaTime / fadeSeconds);

            ApplyFog(_currentWeight);

            if (_currentWeight == 0f && _targetWeight == 0f)
                RestoreOriginalFog();
        }

        private void CaptureOriginalFogIfNeeded()
        {
            if (_hasCapturedOriginalFog)
                return;

            _originalFogEnabled = RenderSettings.fog;
            _originalFogColor = RenderSettings.fogColor;
            _originalFogDensity = RenderSettings.fogDensity;
            _hasCapturedOriginalFog = true;
        }

        private void ApplyFog(float weight)
        {
            RenderSettings.fog = true;
            RenderSettings.fogColor = Color.Lerp(_originalFogColor, _fogColor, weight);
            RenderSettings.fogDensity = Mathf.Lerp(_originalFogDensity, _fogDensity, weight);
        }

        private void RestoreOriginalFog()
        {
            RenderSettings.fog = _originalFogEnabled;
            RenderSettings.fogColor = _originalFogColor;
            RenderSettings.fogDensity = _originalFogDensity;
            _hasCapturedOriginalFog = false;
        }
    }
}
