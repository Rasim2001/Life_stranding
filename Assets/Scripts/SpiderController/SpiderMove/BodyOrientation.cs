using Infastructure.Services.CameraProvider;
using UnityEngine;
using Zenject;

namespace SpiderController.SpiderMove
{
    public class BodyOrientation : MonoBehaviour
    {
        [SerializeField] private Transform _legsRootBone;
        [SerializeField] private Transform _headRootBone;
        [SerializeField] private Transform _raycastRig;

        private Vector3 _legsRootDefaultLocalEuler;
        private Vector3 _headDefaultLocalEuler;
        private Vector3 _raycastDefaultLocalEuler;

        private ICameraProviderService _cameraProviderService;

        [Inject]
        public void Construct(ICameraProviderService cameraProviderService) =>
            _cameraProviderService = cameraProviderService;

        private void Awake()
        {
            if (_legsRootBone != null)
                _legsRootDefaultLocalEuler = _legsRootBone.localEulerAngles;

            if (_headRootBone != null)
                _headDefaultLocalEuler = _headRootBone.localEulerAngles;

            if (_raycastRig != null)
                _raycastDefaultLocalEuler = _raycastRig.localEulerAngles;
        }

        private void Update()
        {
            Vector3 camForward = Vector3.ProjectOnPlane(
                _cameraProviderService.CameraTransform.forward,
                _cameraProviderService.CameraTransform.up).normalized;

            RotateTo(camForward);
        }

        private void RotateTo(Vector3 direction)
        {
            Vector3 flatDir = Vector3.ProjectOnPlane(direction, _cameraProviderService.CameraTransform.up);

            if (flatDir.sqrMagnitude < 0.0001f)
                return;

            flatDir.Normalize();

            Quaternion targetWorldRot = Quaternion.LookRotation(flatDir, _cameraProviderService.CameraTransform.up);

            float speed = 2f;

            RotateBoneLocalY(_legsRootBone, _legsRootDefaultLocalEuler, targetWorldRot, speed);
            RotateBoneLocalY(_headRootBone, _headDefaultLocalEuler, targetWorldRot, speed);
            RotateBoneLocalY(_raycastRig, _raycastDefaultLocalEuler, targetWorldRot, speed);
        }

        private void RotateBoneLocalY(Transform bone, Vector3 defaultLocalEuler, Quaternion targetWorldRot, float speed)
        {
            if (bone == null)
                return;

            Transform parent = bone.parent;

            Quaternion targetLocalRot = parent != null
                ? Quaternion.Inverse(parent.rotation) * targetWorldRot
                : targetWorldRot;

            Vector3 targetLocalEuler = targetLocalRot.eulerAngles;

            float targetY = targetLocalEuler.y;

            Vector3 currentEuler = bone.localEulerAngles;
            float newY = Mathf.LerpAngle(currentEuler.y, targetY + defaultLocalEuler.y, Time.deltaTime * speed);

            Vector3 finalEuler = new Vector3(
                defaultLocalEuler.x,
                newY,
                defaultLocalEuler.z
            );

            bone.localRotation = Quaternion.Euler(finalEuler);
        }
    }
}