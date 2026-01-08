using System;
using System.Linq;
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
        public event Action<bool> OnCutsceneActiveChanged;
        public event Action OnSkipHappened;
        public CutsceneId CutsceneId { get; private set; }

        private ICutSceneRunner _cutSceneRunner;


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
        private DiContainer _diContainer;

        public CutSceneService(IStaticDataService staticDataService, DiContainer diContainer)
        {
            _diContainer = diContainer;
            _staticDataService = staticDataService;
        }


        public async UniTask StartCutsceneAsync(CutsceneId cutsceneId)
        {
            CutsceneId = cutsceneId;

            GameObject prefab = _staticDataService.CutScenesStaticData.Cutscenes
                .First(x => x.Key == cutsceneId).Value;

            GameObject cutSceneObject = _diContainer.InstantiatePrefab(prefab);
            _cutSceneRunner = cutSceneObject.GetComponent<ICutSceneRunner>();

            await Run();

            Object.Destroy(cutSceneObject);
        }

        public void StartCutscene() =>
            Run().Forget();

        private async UniTask Run()
        {
            IsActive = true;

            await _cutSceneRunner.PlayAsync();
            await UniTask.Delay(TimeSpan.FromSeconds(_cutSceneRunner.BlendingTime));

            IsActive = false;
        }
    }
}