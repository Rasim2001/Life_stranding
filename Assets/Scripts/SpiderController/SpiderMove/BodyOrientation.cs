using System;
using Infastructure.Services.CameraProvider;
using Infastructure.Services.PlayerInput;
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
        private IInputService _inputService;
        private Quaternion _targetWorldRot;

        public Quaternion LegsRootBoneRotation =>
            _legsRootBone != null ? _legsRootBone.localRotation : Quaternion.identity;

        public Quaternion RaycastRigRotation =>
            _raycastRig != null ? _raycastRig.localRotation : Quaternion.identity;

        public Quaternion HeadRootRotation =>
            _headRootBone != null ? _headRootBone.localRotation : Quaternion.identity;

        [Inject]
        public void Construct(ICameraProviderService cameraProviderService, IInputService inputService)
        {
            _inputService = inputService;
            _cameraProviderService = cameraProviderService;
        }

        public void Freeze() => enabled = false;
        public void Unfreeze() => enabled = true;


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
            if (_inputService.InputVector.sqrMagnitude <= Mathf.Epsilon)
                return;

            Vector3 camForward = Vector3.ProjectOnPlane(
                _cameraProviderService.CameraTransform.forward,
                _cameraProviderService.CameraTransform.up).normalized;

            Vector3 flatDir = Vector3.ProjectOnPlane(camForward, _cameraProviderService.CameraTransform.up);

            if (flatDir.sqrMagnitude < 0.0001f)
                return;

            flatDir.Normalize();

            _targetWorldRot = Quaternion.LookRotation(flatDir, _cameraProviderService.CameraTransform.up);
        }

        private void FixedUpdate()
        {
            float speed = 2f;

            RotateBoneLocalY(_legsRootBone, _legsRootDefaultLocalEuler, _targetWorldRot, speed);
            RotateBoneLocalY(_headRootBone, _headDefaultLocalEuler, _targetWorldRot, speed);
            RotateBoneLocalY(_raycastRig, _raycastDefaultLocalEuler, _targetWorldRot, speed);
        }

        public void SetBonesRotation(Quaternion legsRootRotation, Quaternion raycastRigRotation, Quaternion headRootRotation)
        {
            _legsRootBone.localRotation = legsRootRotation;
            _raycastRig.localRotation = raycastRigRotation;
            _headRootBone.localRotation = headRootRotation;
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
            float newY = Mathf.LerpAngle(currentEuler.y, targetY + defaultLocalEuler.y, Time.fixedDeltaTime * speed);

            Vector3 finalEuler = new Vector3(
                defaultLocalEuler.x,
                newY,
                defaultLocalEuler.z
            );

            bone.localRotation = Quaternion.Euler(finalEuler);
        }
    }
}