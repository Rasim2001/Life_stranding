using System.Collections.Generic;
using System.Linq;
using Common.Biosphere;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Infastructure.Data;
using Infastructure.Services.SaveLoadService;
using Infastructure.Services.Window;
using PickupObjects.PickUpOnPlatform.FlowerManagement;
using UI;
using UnityEngine;
using Zenject;

namespace Common
{
    public class CheckPoint : MonoBehaviour, ICheckpointInfo, ISavedProgress
    {
        [SerializeField] private BiosphereFx _biosphereFx;
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
        private bool _isPutdownInProgress;

        private float _antennaPieceOffset;
        private Color _defaultColor;

        private IWindowService _windowService;
        private ISaveLoadService _saveLoadService;
        private MarkerUniqueId _markerUniqueId;

        private bool _wasPicked;

        [Inject]
        public void Construct(IWindowService windowService, ISaveLoadService saveLoadService)
        {
            _saveLoadService = saveLoadService;
            _windowService = windowService;
        }


        private void Awake()
        {
            _markerUniqueId = GetComponent<MarkerUniqueId>();

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

        public void LoadProgress(PlayerProgress progress)
        {
            List<CheckpointData> list = progress.WorldProgressData.CheckpointDatas;

            CheckpointData existing = list.FirstOrDefault(x => x.UniqueId == _markerUniqueId.UniqueId);

            if (existing != null && existing.WasPicked)
                _biosphereFx.ShowFx(1);

            if (existing == null || !existing.IsReady)
                return;

            IsReady = true;

            foreach (MeshRenderer indicator in _healIndicators)
                indicator.material.color = Color.red;
        }

        public void UpdateProgress(PlayerProgress progress)
        {
            List<CheckpointData> list = progress.WorldProgressData.CheckpointDatas;

            CheckpointData existing = list.FirstOrDefault(x => x.UniqueId == _markerUniqueId.UniqueId);
            bool isReadyForSave = _isPutdownInProgress || IsReady;

            if (existing == null)
                list.Add(new CheckpointData(isReadyForSave, _markerUniqueId.UniqueId, _wasPicked));
            else
            {
                existing.IsReady = isReadyForSave;
                existing.WasPicked = _wasPicked;
            }
        }

        private void OnDestroy()
        {
            _observerTrigger.OnTriggerEnterHappened -= TriggerEnter;

            Clear();
        }

        public void StartFlowerPutdown(Flower flower)
        {
            if (!_wasPicked)
                _wasPicked = true;

            StartFlowerPutdownAsync(flower).Forget();
        }

        public void StartFlowerPickup() =>
            StartFlowerPickupAsync().Forget();

        private void TriggerEnter(Collider obj)
        {
            RotateBodyToSpider(obj);

            if (_isLaunched)
                return;

            _isLaunched = true;

            _biosphereFx.ShowFx(1);
            DeployAntenna();
            OpenWindow();

            _saveLoadService.SaveProgress();
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

        private async UniTask StartFlowerPutdownAsync(Flower flower)
        {
            _isPutdownInProgress = true;

            Tween startRotateGlassTween = RotateGlass(-180);
            Tween redHealIndicatorTween = ShowHealIndicators();

            await DOTween.Sequence()
                .Append(startRotateGlassTween)
                .Join(redHealIndicatorTween)
                .AsyncWaitForCompletion()
                .AsUniTask();

            flower.ResetFlowerVariant();
            Tween endRotateGlassTween = RotateGlass(0);

            await DOTween.Sequence()
                .Append(endRotateGlassTween)
                .AsyncWaitForCompletion()
                .AsUniTask();

            _isPutdownInProgress = false;
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