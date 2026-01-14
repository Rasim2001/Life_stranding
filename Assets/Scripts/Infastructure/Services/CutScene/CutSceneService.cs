using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infastructure.CutScenes;
using Infastructure.CutScenes.FlowerPickupCutscene;
using Infastructure.StaticData.StaticDataService;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace Infastructure.Services.CutScene
{
    public class CutSceneService : ICutSceneService
    {
        private readonly IStaticDataService _staticDataService;
        private readonly DiContainer _diContainer;
        public event Action<bool> OnCutsceneActiveChanged;
        public event Action OnSkipHappened;
        public CutsceneId CutsceneId { get; private set; }

        private ICutSceneRunner _cutSceneRunner;

        private CancellationTokenSource _currentCts;

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                    OnCutsceneActiveChanged?.Invoke(value);

                _isActive = value;
            }
        }

        private bool _isActive;
        private GameObject _cutSceneObject;

        public CutSceneService(IStaticDataService staticDataService, DiContainer diContainer)
        {
            _diContainer = diContainer;
            _staticDataService = staticDataService;
        }


        public async UniTask StartCutsceneAsync(CutsceneId cutsceneId, CancellationToken ct = default)
        {
            CancelCurrent();

            CutsceneId = cutsceneId;

            _currentCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            CancellationToken linkedToken = _currentCts.Token;

            try
            {
                linkedToken.ThrowIfCancellationRequested();

                GameObject prefab = _staticDataService.CutScenesStaticData.Cutscenes
                    .First(x => x.Key == cutsceneId).Value;

                _cutSceneObject = _diContainer.InstantiatePrefab(prefab);
                _cutSceneRunner = _cutSceneObject.GetComponent<ICutSceneRunner>();

                await Run(linkedToken);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Cutscene canceled");
            }
            catch (Exception e)
            {
                Debug.Log($"Cutscene canceled : {e.Message}");
            }
            finally
            {
                CleanupCutsceneObject();

                IsActive = false;

                _currentCts?.Dispose();
                _currentCts = null;
            }

            Object.Destroy(_cutSceneObject);
        }

        private void CancelCurrent()
        {
            if (_currentCts == null)
                return;

            try
            {
                _currentCts.Cancel();
            }
            catch
            {
                // TODO:
            }
        }

        private void CleanupCutsceneObject()
        {
            _cutSceneRunner = null;

            if (_cutSceneObject != null)
            {
                Object.Destroy(_cutSceneObject);
                _cutSceneObject = null;
            }
        }

        private async UniTask Run(CancellationToken linkedToken)
        {
            IsActive = true;

            await _cutSceneRunner.PlayAsync(linkedToken);
            await UniTask.Delay(TimeSpan.FromSeconds(_cutSceneRunner.BlendingTime), cancellationToken: linkedToken);

            IsActive = false;
        }
    }
}