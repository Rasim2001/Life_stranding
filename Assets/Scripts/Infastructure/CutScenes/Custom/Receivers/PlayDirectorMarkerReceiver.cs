using Infastructure.CutScenes.Custom.Markers;
using Infastructure.Services.CutScene;
using Infastructure.Services.PlayerInput;
using Infastructure.Services.PlayerInput.InputSourceRealization;
using SpiderController;
using UnityEngine;
using UnityEngine.Playables;
using Zenject;

namespace Infastructure.CutScenes.Custom.Receivers
{
    public class PlayDirectorMarkerReceiver : MonoBehaviour, INotificationReceiver
    {
        [SerializeField] private Transform _lastTarget;

        private IInputService _inputService;
        private LockInputSource _lockInputSource;

        private Spider _spider;
        private Transform _moveTarget;
        private Transform _lookingTarget;
        private ICutSceneService _cutSceneService;


        [Inject]
        public void Construct(IInputService inputService, ICutSceneService cutSceneService)
        {
            _cutSceneService = cutSceneService;
            _inputService = inputService;
        }

        private void Awake()
        {
            _lockInputSource = _inputService.GetInputSource<LockInputSource>();

            _cutSceneService.OnSkipHappened += SkipCutScene;
        }

        private void OnDestroy() =>
            _cutSceneService.OnSkipHappened -= SkipCutScene;

        public void Initialize(Spider spider) =>
            _spider = spider;

        private void SkipCutScene() =>
            TeleportTo(_lastTarget);

        public void OnNotify(Playable origin, INotification notification, object context)
        {
            PlayableDirector director = origin.GetGraph().GetResolver() as PlayableDirector;

            if (notification is ExplosionMarker explosionMarker)
            {
                /*Transform target = explosionMarker.Target.Resolve(director);
                if (target)
                    _spider.SpiderImpactReceiver.ApplyExplosionForce(target.position, explosionMarker.Force,
                        explosionMarker.Radius);*/
            }
            else if (notification is MoveToTargetMarker moveToTargetMarker)
            {
                Transform target = moveToTargetMarker.Target.Resolve(director);
                if (target) MoveTo(target);
            }
            else if (notification is LookAtTargetMarker lookAtTarget)
            {
                Transform target = lookAtTarget.Target.Resolve(director);
                if (target) LookAt(target);
            }
            else if (notification is TeleportToTargetMarker teleportTo)
            {
                Transform target = teleportTo.Target.Resolve(director);
                if (target) TeleportTo(target);
            }
            else if (notification is ResetAllTargetsMarker)
            {
                _moveTarget = null;
                _lookingTarget = null;

                _lockInputSource.InputVector = Vector3.zero;
            }
        }

        private void Update()
        {
            if (_spider == null)
                return;

            if (_moveTarget != null)
                Move();

            if (_lookingTarget != null)
                LookAtTarget();
        }

        private void Move()
        {
            if (Vector3.Distance(_moveTarget.position, _spider.transform.position) < 1)
            {
                _lockInputSource.InputVector = Vector3.zero;
                _moveTarget = null;
                return;
            }

            Vector3 worldDirection = (_moveTarget.position - _spider.transform.position).normalized;

            Vector3 localDirection = _spider.transform.InverseTransformDirection(worldDirection);
            _lockInputSource.InputVector = new Vector3(localDirection.x, 0f, Mathf.Abs(localDirection.z));
        }

        private void LookAtTarget()
        {
            /*Vector3 toTarget = (_lookingTarget.position - _spider.transform.position).normalized;

            Vector3 localDirection = _spider.transform.InverseTransformDirection(toTarget);
            _cutSceneInputSource.InputVector = new Vector3(localDirection.x, _cutSceneInputSource.InputVector.y,
                _cutSceneInputSource.InputVector.z);

            float angle = Vector3.Angle(_spider.transform.forward, toTarget);

            if (angle < 10)
            {
                _cutSceneInputSource.InputVector = Vector3.zero;
                _cutSceneService.LerpForwardSpeed = 0;
                _lookingTarget = null;
            }*/
        }

        private void MoveTo(Transform target) =>
            _moveTarget = target;

        private void LookAt(Transform target)
        {
            /*_moveTarget = null;
            _cutSceneService.LerpForwardSpeed = 120;

            _lookingTarget = target;*/
        }

        private void TeleportTo(Transform target)
        {
            /*_lookingTarget = null;
            _moveTarget = null;
            _cutSceneInputSource.InputVector = Vector3.zero;
            _cutSceneService.LerpForwardSpeed = 0;

            _spider.enabled = false;
            _spider.Rigidbody.linearVelocity = Vector3.zero;
            _spider.Rigidbody.angularVelocity = Vector3.zero;

            _spider.transform.position = target.transform.position;
            _spider.transform.rotation = target.transform.rotation;

            _spider.ForceLegsAfterTeleport();
            _spider.enabled = true;*/
        }
    }
}