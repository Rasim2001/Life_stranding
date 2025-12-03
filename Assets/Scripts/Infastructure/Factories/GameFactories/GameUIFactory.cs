using Infastructure.Common;
using Infastructure.Services.QTE;
using PickupObjects.PickUpOnPlatform.FlowerManagement;
using UI.MVVM.Base;
using UI.MVVM.View.Root;
using Unity.VisualScripting;
using UnityEngine;
using Zenject;

namespace Infastructure.Factories.GameFactories
{
    public class GameUIFactory : IGameUIFactory
    {
        private readonly IUIRoot _uiRoot;
        private readonly DiContainer _diContainer;
        private readonly UIGameplayRootViewModel _gameplayRootViewModel;

        private IGameUIFactory _iuiFactory;
        private ILastChanceQTEService _lastChanceQteService;

        public GameUIFactory(DiContainer diContainer, UIGameplayRootViewModel gameplayRootViewModel, IUIRoot uiRoot,
            ILastChanceQTEService lastChanceQTEService)
        {
            _lastChanceQteService = lastChanceQTEService;
            _diContainer = diContainer;
            _gameplayRootViewModel = gameplayRootViewModel;
            _uiRoot = uiRoot;
        }

        public void CreateGamplayRoot()
        {
            UIGameplayRootBinder uiGameplayRootBinder =
                _diContainer.InstantiatePrefabResourceForComponent<UIGameplayRootBinder>(AssetsPath.GamePlayUIPath);
            uiGameplayRootBinder.Bind(_gameplayRootViewModel);

            _uiRoot.AttachSceneUI(uiGameplayRootBinder.gameObject);
        }

        public IWindowBinder CreateWindow(WindowViewModel viewModel, Transform container)
        {
            string prefabPath = GetPrefabPath(viewModel);

            GameObject createdPopup = _diContainer.InstantiatePrefabResource(prefabPath, container);
            IWindowBinder binder = createdPopup.GetComponent<IWindowBinder>();

            binder.Bind(viewModel);

            return binder;
        }

        public void CreateLastChanceRoot(Flower flower)
        {
            LastChanceUI lastChance =
                _diContainer.InstantiatePrefabResourceForComponent<LastChanceUI>(AssetsPath.LastChanceRoot);

            lastChance.SetFlowerTransform(flower);

            _lastChanceQteService.Initialize(lastChance);
        }


        private static string GetPrefabPath(WindowViewModel viewModel) =>
            $"Prefabs/UI/Windows/{viewModel.Id}";
    }
}