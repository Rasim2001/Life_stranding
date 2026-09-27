using UnityEngine;

namespace WeatherSystem
{
    // Небесный/облачный купол — всегда вокруг камеры, ровно как у Cozy
    // (CozyWeather.cs: transform.position = cozyCamera.transform.position,
    // scale = farClipPlane / 1000). У CloudLayer тот же паттерн следования, но там
    // держится Y и следит только по XZ — это физический слой мира, сквозь который
    // паук пролетает. Купол не часть мира: он не должен быть "недостижим на какой-то
    // высоте", поэтому едет за камерой по всем трём осям.
    //
    // [ExecuteAlways], которого нет у CloudLayer: художник должен видеть купол
    // и настраивать его в Scene View без входа в Play Mode, как в редакторе Cozy.
    [ExecuteAlways]
    [RequireComponent(typeof(MeshRenderer))]
    public class WeatherDome : MonoBehaviour
    {
        [Tooltip("Множитель поверх farClipPlane камеры, чтобы край меша гарантированно не попадал в кадр.")]
        [SerializeField] private float _sizeMargin = 0.9f;

        private Camera _camera;
        private MeshRenderer _renderer;

        public MeshRenderer Renderer => _renderer != null ? _renderer : (_renderer = GetComponent<MeshRenderer>());

        // LateUpdate — камера в этом кадре уже переместилась, купол подстраивается
        // следом, без кадра запаздывания, который дал бы Update.
        private void LateUpdate()
        {
            Camera cam = ResolveCamera();
            if (cam == null)
                return;

            transform.position = cam.transform.position;

            float scale = cam.farClipPlane * _sizeMargin;
            transform.localScale = new Vector3(scale, scale, scale);
        }

        // В Play Mode — Camera.main (тегированная MainCamera). В Scene View вне Play —
        // текущая камера сцены, тот же приём, что у CozyWeather.cs (Camera.current /
        // SceneView.camera), иначе купол в редакторе стоит на месте, а не следует
        // за навигацией художника.
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
