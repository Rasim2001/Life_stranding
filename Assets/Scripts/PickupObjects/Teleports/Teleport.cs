using DG.Tweening;
using Infastructure.Services.CameraProvider;
using Infastructure.Services.Teleports;
using SpiderController;
using UnityEngine;
using Zenject;

namespace PickupObjects.Teleports
{
    public class Teleport : MonoBehaviour
    {
        [SerializeField] private Camera _portalCamera;
        public Camera PortalCamera => _portalCamera;

        private ITeleportService _teleportService;

        private Teleport _otherTeleport;
        private Tween _scaleTween;
        private bool _isTriggered;

        private MeshRenderer _meshRender;
        private ICameraProviderService _cameraProviderService;

        [Inject]
        public void Construct(ITeleportService teleportService, ICameraProviderService cameraProviderService)
        {
            _cameraProviderService = cameraProviderService;
            _teleportService = teleportService;
        }


        private void Awake()
        {
            _meshRender = GetComponentInChildren<MeshRenderer>();
            _portalCamera.targetTexture = new RenderTexture(Screen.width, Screen.height, 24);
        }


        private void Update()
        {
            if (_otherTeleport == null)
                return;

            Vector3 lookerPosition =
                _otherTeleport.transform.worldToLocalMatrix.MultiplyPoint3x4(_cameraProviderService.CameraTransform.position);
            lookerPosition = new Vector3(-lookerPosition.x, lookerPosition.y, -lookerPosition.z);
            _portalCamera.transform.localPosition = lookerPosition;

            Quaternion difference =
                transform.rotation * Quaternion.Inverse(_otherTeleport.transform.rotation * Quaternion.Euler(0, 180, 0));
            _portalCamera.transform.rotation = difference * _cameraProviderService.CameraTransform.rotation;

            _portalCamera.nearClipPlane = Mathf.Max(0.01f, lookerPosition.magnitude);
        }

        public void Initialize(Vector3 position, float yAngle)
        {
            transform.localScale = Vector3.zero;
            transform.position = position;

            Vector3 currentEuler = transform.rotation.eulerAngles;
            transform.rotation = Quaternion.Euler(currentEuler.x, yAngle, currentEuler.z);

            _scaleTween = transform.DOScale(Vector3.one, 0.25f);
        }

        public void SetOtherTeleport(Teleport otherTeleport)
        {
            if (otherTeleport == null)
                return;

            _otherTeleport = otherTeleport;
            _meshRender.material.mainTexture = _otherTeleport.PortalCamera.targetTexture;
        }


        public void BlockEntry() =>
            _isTriggered = true;

        private void OnDisable()
        {
            _isTriggered = false;

            _scaleTween.Kill();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isTriggered)
                return;

            if (other.TryGetComponent<Spider>(out _))
            {
                _isTriggered = true;

                _teleportService.TryTeleportSpider(this);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent<Spider>(out _))
                _isTriggered = false;
        }
    }
}