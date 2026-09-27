using UnityEngine;

namespace WeatherSystem
{
    // Диск луны — следует за камерой, как WeatherDome, но не окружает её: выносится
    // по направлению на луну на дистанцию, пропорциональную farClipPlane (тот же приём,
    // что у CozySatelliteModule: satHolder.position = camera.position каждый кадр,
    // иначе конечная дистанция до луны "уплывала" бы при движении игрока).
    //
    // [ExecuteAlways] — художник видит и настраивает луну в Scene View без Play Mode.
    [ExecuteAlways]
    [RequireComponent(typeof(MeshRenderer))]
    public class WeatherMoon : MonoBehaviour
    {
        [Tooltip("Источник направления на луну — тот же Moon Light, чьё направление WeatherService публикует как _SR_MoonDirection.")]
        [SerializeField] private Light _moonLight;

        [Tooltip("Доля farClipPlane камеры — дистанция до луны. Меньше купола неба (0.9), чтобы луна не протыкала его изнанку.")]
        [SerializeField] private float _distanceFraction = 0.8f;

        [Tooltip("Угловой размер диска в радианах (диаметр/дистанция) — не абсолютный масштаб, чтобы луна не росла при удалении камеры.")]
        [SerializeField] private float _angularSize = 0.05f;

        [Tooltip("Доворот текстуры после LookRotation — приливный захват: к камере всегда обращена одна и та же сторона.")]
        [SerializeField] private Vector3 _faceOffset = Vector3.zero;

        private Camera _camera;
        private MeshRenderer _renderer;

        public MeshRenderer Renderer => _renderer != null ? _renderer : (_renderer = GetComponent<MeshRenderer>());

        private void LateUpdate()
        {
            Camera cam = ResolveCamera();
            if (cam == null || _moonLight == null)
                return;

            // -forward, не forward: свет "смотрит" от луны к земле, нам нужно направление
            // К луне — тот же приём, что в WeatherService.RotateSunMoonPivot().
            Vector3 moonDirection = -_moonLight.transform.forward;
            float distance = cam.farClipPlane * _distanceFraction;

            transform.position = cam.transform.position + moonDirection * distance;
            transform.rotation = Quaternion.LookRotation(moonDirection) * Quaternion.Euler(_faceOffset);

            float scale = distance * _angularSize;
            transform.localScale = new Vector3(scale, scale, scale);
        }

        // В Play Mode — Camera.main. В Scene View вне Play — текущая камера сцены,
        // тот же приём, что у WeatherDome.cs / CozyWeather.cs.
        private Camera ResolveCamera()
        {
            if (Application.isPlaying)
            {
                if (_camera == null)
                    _camera = Camera.main;
                return _camera;
            }

#if UNITY_EDITOR
            var sceneView = UnityEditor.SceneView.lastActiveSceneView;
            if (sceneView != null && sceneView.camera != null)
                return sceneView.camera;
#endif
            return Camera.main;
        }
    }
}
