using UnityEngine;

namespace WeatherSystem
{
    // Мировой объект, не часть неба. Высота — собственная позиция по Y, задаётся
    // размещением в сцене (per-scene override, как у порогов высоты на WeatherRig).
    //
    // Сам следит только за камерой по XZ и своим размером — держит Y неизменным,
    // это и даёт эффект прохождения сквозь слой. Альфа по высоте паука сюда не входит:
    // её считает WeatherService, тем же способом, что небо/солнце/луну — один источник
    // истины для "где паук относительно полос".
    [RequireComponent(typeof(MeshRenderer))]
    public class CloudLayer : MonoBehaviour
    {
        [Tooltip("Полуширина видимой зоны фейда по высоте, в мировых юнитах.")]
        [SerializeField] private float _fadeDistance = 20f;

        [Tooltip("Множитель поверх farClipPlane камеры, чтобы край меша гарантированно не попадал в кадр.")]
        [SerializeField] private float _sizeMargin = 1.2f;

        private Camera _camera;
        private MeshRenderer _renderer;

        // WeatherService.Initialize() читает Renderer раньше, чем гарантированно
        // отработает Awake() этого компонента — порядок Awake() между разными объектами
        // сцены в Unity не определён. GetComponent здесь дешёв и вызывается редко
        // (Initialize/Dispose), поэтому лениво резолвим вместо того, чтобы зависеть
        // от порядка инициализации.
        public MeshRenderer Renderer => _renderer != null ? _renderer : (_renderer = GetComponent<MeshRenderer>());
        public float BandCenterY => transform.position.y;
        public float FadeDistance => _fadeDistance;

        // LateUpdate — камера в этом кадре уже переместилась, меш подстраивается следом,
        // без кадра запаздывания, который дал бы Update.
        private void LateUpdate()
        {
            if (_camera == null)
                _camera = Camera.main;

            if (_camera == null)
                return;

            Vector3 camPos = _camera.transform.position;
            Vector3 pos = transform.position;
            pos.x = camPos.x;
            pos.z = camPos.z;
            transform.position = pos;

            // Плоскость Unity по умолчанию — 10×10, отсюда половинный размер /5.
            float halfExtent = _camera.farClipPlane * _sizeMargin;
            transform.localScale = new Vector3(halfExtent / 5f, 1f, halfExtent / 5f);
        }
    }
}
