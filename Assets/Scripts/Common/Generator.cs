using DG.Tweening;
using UnityEngine;

namespace Common
{
    public class Generator : MonoBehaviour
    {
        private const string Emission = "_EMISSION";

        [SerializeField] private Material _material;
        [SerializeField] private Transform _putdownBatteryPoint;
        [SerializeField] private Transform _pickUpDisplayPoint;
        [SerializeField] private Transform _rotateTarget;
        [SerializeField] private Transform[] _moveTargets;
        public Transform PickupDisplayPoint => _pickUpDisplayPoint;
        public Transform PutdownBatteryPoint => _putdownBatteryPoint;

        public bool IsLaunched { get; private set; }

        private readonly Vector3 _rotationSpeed = new Vector3(0, 360, 0);

        private Sequence _sequenceMove;
        private Tween _rotateTween;


        public void StartGenerator()
        {
            IsLaunched = true;

            _sequenceMove?.Kill();
            _sequenceMove = DOTween.Sequence();

            _material.EnableKeyword(Emission);

            foreach (Transform moveTarget in _moveTargets)
                _sequenceMove.Join(moveTarget.DOLocalMoveZ(0.45f, 3));

            _sequenceMove.OnComplete(() =>
            {
                _rotateTarget?.DOKill();
                _rotateTarget.DOLocalRotate(_rotationSpeed, 2, RotateMode.LocalAxisAdd)
                    .SetEase(Ease.Linear)
                    .SetLoops(-1, LoopType.Incremental);
            });
        }


        private void OnDestroy()
        {
            _sequenceMove?.Kill();
            _rotateTween?.Kill();
        }
    }
}