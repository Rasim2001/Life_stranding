using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Infastructure.Data;
using Infastructure.Services.GeneratorLaunchTracker;
using Infastructure.Services.SaveLoadService;
using UnityEngine;
using Zenject;

namespace Common
{
    [RequireComponent(typeof(MarkerUniqueId))]
    public class Generator : MonoBehaviour, ISavedProgress
    {
        private const string Emission = "_EMISSION";

        [SerializeField] private BiosphereFx _biosphereFx;
        [SerializeField] private Material _material;
        [SerializeField] private Transform _putdownBatteryPoint;
        [SerializeField] private Transform _pickUpDisplayPoint;
        [SerializeField] private Transform _rotateTarget;
        [SerializeField] private Transform[] _moveTargets;
        public Transform PickupDisplayPoint => _pickUpDisplayPoint;
        public Transform PutdownBatteryPoint => _putdownBatteryPoint;

        public bool IsLaunched { get; private set; }

        private readonly Vector3 _rotationSpeed = new Vector3(0, 360, 0);

        private IGeneratorLaunchTrackerService _generatorLaunchTrackerService;
        private Sequence _sequenceMove;
        private MarkerUniqueId _markerUniqueId;

        [Inject]
        public void Construct(IGeneratorLaunchTrackerService generatorLaunchTrackerService) =>
            _generatorLaunchTrackerService = generatorLaunchTrackerService;

        private void Awake() =>
            _markerUniqueId = GetComponent<MarkerUniqueId>();

        private void Start()
        {
            if (IsLaunched)
                _material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        public void LoadProgress(PlayerProgress progress)
        {
            GeneratorData existing = progress.WorldProgressData.GeneratorDatas.FirstOrDefault(x =>
                x.UniqueId == _markerUniqueId.UniqueId);

            if (existing == null)
                return;

            if (existing.IsLaunched)
                StartGenerator();
        }

        public void UpdateProgress(PlayerProgress progress)
        {
            List<GeneratorData> list = progress.WorldProgressData.GeneratorDatas;

            GeneratorData existing = list.FirstOrDefault(x => x.UniqueId == _markerUniqueId.UniqueId);

            if (existing == null)
                progress.WorldProgressData.GeneratorDatas.Add(new GeneratorData(_markerUniqueId.UniqueId, IsLaunched));
            else
                existing.IsLaunched = IsLaunched;
        }

        public void StartGenerator()
        {
            IsLaunched = true;

            _generatorLaunchTrackerService.Launch();
            _biosphereFx.ShowFx(1);
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

        public async UniTask StartGeneratorAsync()
        {
            IsLaunched = true;

            await UniTask.Delay(TimeSpan.FromSeconds(0.5f));

            _biosphereFx.ShowFx(1);
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


        private void OnDestroy() =>
            _sequenceMove?.Kill();
    }
}