using Infastructure.Factories;
using Infastructure.StaticData.Spider;
using Infastructure.StaticData.StaticDataService;
using MoreMountains.Feedbacks;
using MoreMountains.FeedbacksForThirdParty;
using Unity.Cinemachine;
using UnityEngine;
using Zenject;

namespace Cameras.SpiderCameras
{
    public class SpiderCamera : MonoBehaviour, ISpiderCamera
    {
        [SerializeField] Transform _pivot;

        [SerializeField] private CinemachineRotationComposer _rotationComposer;
        [SerializeField] private CinemachineThirdPersonFollow _thirdPersonFollow;
        [SerializeField] private CinemachineCamera _cinemachineCamera;
        [SerializeField] private MMF_Player _cameraShake;

        [Header("Debug")]
        [Tooltip("Draws where the camera is actually aiming, in the Scene view during play. " +
                 "Tuning aid only — editor-only code, nothing ships with it.")]
        [SerializeField] private bool _showAimTarget;

        [Tooltip("Same aim point, drawn as a ring directly over the rendered frame instead of the " +
                 "Scene view — for judging framing against what the player actually sees.")]
        [SerializeField] private bool _showAimTargetOnScreen;

        public Vector3 ShoulderOffset
        {
            get => _thirdPersonFollow.ShoulderOffset;
            set => _thirdPersonFollow.ShoulderOffset = value;
        }


        public float FieldOfView
        {
            get => _cinemachineCamera.Lens.FieldOfView;
            set => _cinemachineCamera.Lens.FieldOfView = value;
        }

        public float Distance
        {
            get => _thirdPersonFollow.CameraDistance;
            set => _thirdPersonFollow.CameraDistance = value;
        }

        private float _authoredScreenPositionY;

        public float FramingVerticalOffset
        {
            get => _rotationComposer.Composition.ScreenPosition.y - _authoredScreenPositionY;
            set
            {
                var composition = _rotationComposer.Composition;
                Vector2 screenPosition = composition.ScreenPosition;
                screenPosition.y = _authoredScreenPositionY + value;
                composition.ScreenPosition = screenPosition;
                _rotationComposer.Composition = composition;
            }
        }

        public float AimHeight
        {
            get => _rotationComposer.TargetOffset.y;
            set
            {
                Vector3 targetOffset = _rotationComposer.TargetOffset;
                targetOffset.y = value;
                _rotationComposer.TargetOffset = targetOffset;
            }
        }

        public float AimForward
        {
            get => _rotationComposer.TargetOffset.z;
            set
            {
                Vector3 targetOffset = _rotationComposer.TargetOffset;
                targetOffset.z = value;
                _rotationComposer.TargetOffset = targetOffset;
            }
        }

        private MMF_CinemachineImpulse Impulse =>
            _cameraShake.GetFeedbackOfType<MMF_CinemachineImpulse>();

        private IStaticDataService _staticData;
        private IDiFactory _diFactory;

        private SpiderCameraFollower _follower;
        private SpiderCameraFov _fov;
        private SpiderCameraOrbit _orbit;
        private SpiderCameraDistance _distance;


        [Inject]
        public void Construct(IStaticDataService staticData, IDiFactory diFactory)
        {
            _diFactory = diFactory;
            _staticData = staticData;
        }

        public void Initialize()
        {
            _authoredScreenPositionY = _rotationComposer.Composition.ScreenPosition.y;

            _follower = _diFactory.Create<SpiderCameraFollower>(_pivot);
            _fov = _diFactory.Create<SpiderCameraFov>(this);
            _distance = _diFactory.Create<SpiderCameraDistance>(this);

            _orbit = _diFactory.Create<SpiderCameraOrbit>(this, _pivot);
            _orbit.Initialize();

            // Without this the camera keeps whatever yaw was authored on the CameraFollower prefab
            // object — StartInput() inside _orbit.Initialize() has already used it as the orbit's
            // starting pose, so by this point it's baked into the pivot's rotation. Same snap+align
            // order TeleportService uses.
            _follower.Snap();
            _orbit.AlignToSpider();
        }


        public void ShakeCamera(float distanceFalling)
        {
            SpiderStaticData data = _staticData.SpiderStaticData;

            float distanceNormalized = Mathf.InverseLerp(
                data.MinShakeDistance,
                data.MaxShakeDistance,
                distanceFalling);

            float force = Mathf.Lerp(data.MinForceShake, data.MaxForceShake, distanceNormalized);

            Impulse.m_ImpulseDefinition.FrequencyGain = force;
            Impulse.m_ImpulseDefinition.AmplitudeGain = force;

            _cameraShake.PlayFeedbacks();
        }

        public void AlignToSpider() =>
            _orbit.AlignToSpider();

        public void SnapToTarget() =>
            _follower.Snap();

        private void OnDestroy()
        {
            _orbit.Destroy();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// Where the composer is actually aiming, computed the same way
        /// CinemachineRotationComposer does it internally — lookAt + lookAtRotation * TargetOffset —
        /// rather than read back off the camera, so both debug views stay correct even on a frame
        /// the camera has not solved yet. Shared so the Scene-view gizmo and the on-screen ring
        /// can't drift apart into showing two different points.
        /// </summary>
        private bool TryGetAimPoint(out Vector3 spider, out Vector3 aim)
        {
            spider = Vector3.zero;
            aim = Vector3.zero;

            if (_rotationComposer == null || _pivot == null)
                return false;

            spider = _pivot.position;
            aim = spider + _pivot.rotation * _rotationComposer.TargetOffset;
            return true;
        }

        private static Texture2D _ringTexture;

        private static Texture2D RingTexture()
        {
            if (_ringTexture != null)
                return _ringTexture;

            // Transparent inside, solid on the rim — tinted per-draw via GUI.color rather than
            // baking colour in, so one texture serves both the aim ring and the spider ring.
            const int size = 32;
            const float outerR = size * 0.5f - 1f;
            const float innerR = outerR - 3f;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 centre = new Vector2(size * 0.5f, size * 0.5f);

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), centre);
                bool onRim = d <= outerR && d >= innerR;
                tex.SetPixel(x, y, onRim ? Color.white : Color.clear);
            }

            tex.Apply();
            tex.hideFlags = HideFlags.HideAndDontSave;
            _ringTexture = tex;
            return _ringTexture;
        }

        /// <summary>
        /// Same aim point as the gizmo above, drawn over the actual rendered frame instead of the
        /// Scene view — for judging where the aim offsets land the composer against what the player
        /// sees, not against the sideways Scene camera. Points behind the camera are skipped rather
        /// than drawn: WorldToScreenPoint mirrors a negative-z point onto the visible screen instead
        /// of reporting it off-frame, so drawing it unconditionally would show a false ring.
        /// </summary>
        private void OnGUI()
        {
            if (!_showAimTargetOnScreen)
                return;

            Camera cam = Camera.main;
            if (cam == null || !TryGetAimPoint(out Vector3 spider, out Vector3 aim))
                return;

            DrawRing(cam, aim, Color.yellow, 22f);
            DrawRing(cam, spider, Color.white, 14f);

            Vector3 offset = _rotationComposer.TargetOffset;
            GUI.color = Color.yellow;
            GUI.Label(new Rect(10, 10, 300, 20), $"AimHeight={offset.y:F2}  AimForward={offset.z:F2}");
            GUI.color = Color.white;
        }

        private void DrawRing(Camera cam, Vector3 worldPos, Color color, float pixelSize)
        {
            Vector3 screen = cam.WorldToScreenPoint(worldPos);
            if (screen.z < 0f)
                return;

            Rect rect = new Rect(
                screen.x - pixelSize * 0.5f,
                Screen.height - screen.y - pixelSize * 0.5f,
                pixelSize, pixelSize);

            GUI.color = color;
            GUI.DrawTexture(rect, RingTexture());
            GUI.color = Color.white;
        }
#endif

#if UNITY_EDITOR
        /// <summary>
        /// Tuning aid: shows where the camera is actually aiming, in the Scene view during play.
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!_showAimTarget || !TryGetAimPoint(out Vector3 spider, out Vector3 aim))
                return;

            // Where the spider itself is — the aim point's reference.
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(spider, 0.12f);

            // The offset the aim point carries, and the aim point itself.
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(spider, aim);
            Gizmos.DrawSphere(aim, 0.15f);

            Camera cam = Camera.main;
            if (cam == null)
                return;

            // Camera to aim point: the line the composer is pinning to its screen position.
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(cam.transform.position, aim);

            Vector3 offset = _rotationComposer.TargetOffset;
            Vector3 shoulder = _thirdPersonFollow != null ? _thirdPersonFollow.ShoulderOffset : Vector3.zero;
            float orbitAngle = Mathf.Atan2(shoulder.y, -shoulder.z) * Mathf.Rad2Deg;

            UnityEditor.Handles.color = Color.yellow;
            UnityEditor.Handles.Label(
                aim + Vector3.up * 0.25f,
                $"aim  h={offset.y:F2}  fwd={offset.z:F2}\n" +
                $"orbit {orbitAngle:F1}°  radius {new Vector2(shoulder.y, shoulder.z).magnitude:F2}");
        }
#endif

        private void Update()
        {
            if (_orbit == null)
                return;

            _follower.Update();
            _fov.Update();
            _orbit.Update();
            _distance.Update();
        }

        private void FixedUpdate()
        {
            if (_follower == null || _fov == null || _orbit == null)
                return;

            _follower.FixedUpdate();
        }
    }
}