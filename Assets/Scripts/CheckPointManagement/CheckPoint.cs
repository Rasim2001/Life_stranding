using System;
using System.Linq;
using Common;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Infastructure.Services.Window;
using UI;
using UnityEngine;
using VInspector;
using Zenject;

namespace CheckPointManagement
{
    public class CheckPoint : MonoBehaviour
    {
        [SerializeField] private ObserverTrigger _observerTrigger;
        [SerializeField] private Transform _flowerPutdownPoint;
        [SerializeField] private Transform _bodyTransform;
        [SerializeField] private Transform _glassTransform;
        [SerializeField] private Transform[] _antenna;

        [SerializeField] private MeshRenderer[] _healIndicators;
        public Vector3 FlowerPutdownPosition => _flowerPutdownPoint.position;
        public Quaternion FlowerPutdownRotation => _flowerPutdownPoint.rotation;
        public bool IsReady { get; private set; }

        private Sequence _antennaSequence;
        private Sequence _healIndicatorsSequence;

        private Tween _rotateBodyTween;
        private Tween _rotateGlassTween;

        private bool _isLaunched;

        private float _antennaPieceOffset;
        private Color _defaultColor;

        private IWindowService _windowService;

        [Inject]
        public void Construct(IWindowService windowService) =>
            _windowService = windowService;

        private void Awake()
        {
            _antennaPieceOffset = _antenna.FirstOrDefault().localPosition.z;
            _defaultColor = _healIndicators.FirstOrDefault().material.color;

            foreach (Transform piece in _antenna)
            {
                piece.transform.localPosition =
                    new Vector3(piece.transform.localPosition.x, piece.transform.localPosition.y, 0);
            }

            foreach (MeshRenderer meshRenderer in _healIndicators)
                meshRenderer.material = new Material(meshRenderer.material);
        }

        private void Start() =>
            _observerTrigger.OnTriggerEnterHappened += TriggerEnter;

        private void OnDestroy()
        {
            _observerTrigger.OnTriggerEnterHappened -= TriggerEnter;

            Clear();
        }

        public void StartFlowerPutdown() =>
            StartFlowerPutdownAsync().Forget();

        public void StartFlowerPickup() =>
            StartFlowerPickupAsync().Forget();

        private void TriggerEnter(Collider obj)
        {
            RotateBodyToSpider(obj);

            if (_isLaunched)
                return;

            _isLaunched = true;

            DeployAntenna();
            OpenWindow();
        }

        private void OpenWindow() =>
            _windowService.OpenTaskPopup(TaskId.CheckpointDescriptionTask);

        private void RotateBodyToSpider(Collider obj)
        {
            Vector3 dir = obj.transform.position - _bodyTransform.position;
            float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg - 90;

            _rotateBodyTween?.Kill();
            _rotateBodyTween = _bodyTransform.DOLocalRotate(new Vector3(0f, 0, angle), 1f);
        }

        private async UniTask StartFlowerPutdownAsync()
        {
            Tween startRotateGlassTween = RotateGlass(-180);
            Tween redHealIndicatorTween = ShowHealIndicators();

            await DOTween.Sequence()
                .Append(startRotateGlassTween)
                .Join(redHealIndicatorTween)
                .AsyncWaitForCompletion()
                .AsUniTask();

            Tween endRotateGlassTween = RotateGlass(0);

            await DOTween.Sequence()
                .Append(endRotateGlassTween)
                .AsyncWaitForCompletion()
                .AsUniTask();

            IsReady = true;
        }

        private async UniTask StartFlowerPickupAsync()
        {
            Tween hideHealIndicators = HideHealIndicators();

            await DOTween.Sequence()
                .Append(hideHealIndicators)
                .AsyncWaitForCompletion()
                .AsUniTask();

            IsReady = false;
        }

        private Tween RotateGlass(float angle)
        {
            _rotateGlassTween?.Kill();
            _rotateGlassTween = _glassTransform.DOLocalRotate(new Vector3(0, 0, angle), 1).SetEase(Ease.Linear);

            return _rotateGlassTween;
        }

        private void DeployAntenna()
        {
            _antennaSequence?.Kill();
            _antennaSequence = DOTween.Sequence();

            float currentPosition = 0;

            foreach (Transform piece in _antenna)
            {
                currentPosition += _antennaPieceOffset;

                _antennaSequence.Append(piece.transform.DOLocalMoveZ(currentPosition, 0.25f));
            }
        }

        private Tween ShowHealIndicators()
        {
            _healIndicatorsSequence?.Kill();
            _healIndicatorsSequence = DOTween.Sequence();

            foreach (MeshRenderer indicator in _healIndicators)
                _healIndicatorsSequence.Append(indicator.material.DOColor(Color.red, 1));

            return _healIndicatorsSequence;
        }

        private Tween HideHealIndicators()
        {
            _healIndicatorsSequence?.Kill();
            _healIndicatorsSequence = DOTween.Sequence();

            foreach (MeshRenderer indicator in _healIndicators.Reverse())
                _healIndicatorsSequence.Append(indicator.material.DOColor(_defaultColor, 1));

            return _healIndicatorsSequence;
        }


        private void Clear()
        {
            _antennaSequence?.Kill();
            _rotateGlassTween?.Kill();
            _rotateBodyTween?.Kill();
        }
    }
}