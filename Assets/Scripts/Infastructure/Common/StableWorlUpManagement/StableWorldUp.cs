using Infastructure.Services.CameraProvider;
using Infastructure.StaticData.Spider;
using Infastructure.StaticData.StaticDataService;
using Unity.Cinemachine;
using UnityEngine;
using Zenject;

namespace Infastructure.Common.StableWorlUpManagement
{
    public class StableWorldUp : MonoBehaviour, IStableWorldUp
    {
        public Transform StableWorldUpTransform => this ? transform : null;

        private SpiderStaticData SpiderStaticData => _staticDataService.SpiderStaticData;

        private IStaticDataService _staticDataService;
        private CinemachineBrain _cinemachineBrain;
        private ICameraProviderService _cameraProviderService;

        // The horizon eases toward _committedUp every single frame and toward nothing else. That one
        // rule is what makes a crooked resting horizon impossible: the goal is a stored orientation,
        // so the damped rotation at the bottom always converges on it exactly.
        //
        // An earlier version instead stopped rotating altogether whenever it decided the tilt was
        // uninteresting. Any part-finished turn then froze exactly where it was: measured live,
        // the spider sat flat at 0.3 degrees off world vertical while the horizon stayed stuck at
        // 7.3, which is the visibly tilted horizon this replaces.
        private Vector3 _committedUp;
        private bool _hasCommitted;

        // Where the commitment stood before the current turn began. If the spider ends up settling
        // somewhere that is not actually far from here, the turn was terrain — a jump that happened
        // to land on a ramp — and the commitment goes back rather than adopting the ramp's normal,
        // which would tilt the horizon on exactly the slopes that are supposed to leave it alone.
        private Vector3 _preFollowUp;

        // The commitment is only re-pointed on a large, sustained reorientation — a wall or a
        // ceiling — never a rock or a stair step, which is the whole point of holding it.
        private bool _isFollowing;
        private float _aboveEnterTime;

        // Settle detector: the spider counts as settled once its up has stayed inside a small cone
        // for a while. Following ends on the spider settling rather than on the horizon catching up,
        // because catching up happens constantly mid-turn — climbing a wall, the horizon draws level
        // with the spider somewhere around halfway, and committing there would leave the horizon
        // half-turned with no later check large enough to correct it.
        private Vector3 _settleReferenceUp;
        private float _settleTime;

        [Inject]
        public void Construct(IStaticDataService staticDataService, ICameraProviderService cameraProviderService)
        {
            _cameraProviderService = cameraProviderService;
            _staticDataService = staticDataService;
        }

        public void Initialize()
        {
            _cinemachineBrain = _cameraProviderService.CameraTransform.GetComponent<CinemachineBrain>();
            _cinemachineBrain.WorldUpOverride = transform;
        }

        public void Rotate(Quaternion targetRotation, bool isGrounded)
        {
            Vector3 targetUp = targetRotation * Vector3.up;

            if (!_hasCommitted)
            {
                _committedUp = targetUp;
                _preFollowUp = targetUp;
                _settleReferenceUp = targetUp;
                _hasCommitted = true;
            }

            UpdateSettleState(targetUp);
            UpdateCommittedUp(targetUp, isGrounded);

            Quaternion fromTo = Quaternion.FromToRotation(transform.up, _committedUp);
            Quaternion finalRotation = fromTo * transform.rotation;

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                finalRotation,
                Time.deltaTime * SpiderStaticData.WorldUpSmoothRotation
            );
        }

        private void UpdateSettleState(Vector3 targetUp)
        {
            if (Vector3.Angle(targetUp, _settleReferenceUp) > SpiderStaticData.HorizonSettleAngle)
            {
                _settleReferenceUp = targetUp;
                _settleTime = 0f;
                return;
            }

            _settleTime += Time.deltaTime;
        }

        private void UpdateCommittedUp(Vector3 targetUp, bool isGrounded)
        {
            if (_isFollowing)
            {
                // Track live through the whole turn, so the commitment ends up on the surface the
                // spider actually arrived at rather than one it passed through on the way.
                _committedUp = targetUp;

                // Airborne orientation is not a surface. Measured live after a ceiling-to-floor
                // jump, concluding here froze the commitment 13 degrees off world vertical while
                // the spider went on to land perfectly flat — and 13 degrees is far below the
                // entry angle, so nothing could ever correct it again.
                if (!isGrounded || _settleTime < SpiderStaticData.HorizonSettleTime)
                    return;

                _isFollowing = false;

                if (Vector3.Angle(targetUp, _preFollowUp) < SpiderStaticData.HorizonFollowEnterAngle)
                    _committedUp = _preFollowUp;

                return;
            }

            if (Vector3.Angle(targetUp, _committedUp) < SpiderStaticData.HorizonFollowEnterAngle)
            {
                _aboveEnterTime = 0f;
                return;
            }

            // Compared against the commitment, not against our own current up: the commitment is
            // what the horizon is heading for, so this stays a question about the spider's
            // orientation and cannot degenerate into the horizon measuring itself.
            _aboveEnterTime += Time.deltaTime;

            if (_aboveEnterTime < SpiderStaticData.HorizonFollowDwellTime)
                return;

            _isFollowing = true;
            _preFollowUp = _committedUp;
            _aboveEnterTime = 0f;
            _settleTime = 0f;
        }
    }
}
