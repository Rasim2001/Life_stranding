using Infastructure.Common;
using UI.MVVM.Base;
using UI.MVVM.View.Root;
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

        public GameUIFactory(DiContainer diContainer, UIGameplayRootViewModel gameplayRootViewModel, IUIRoot uiRoot)
        {
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


        private static string GetPrefabPath(WindowViewModel viewModel) =>
            $"Prefabs/UI/Windows/{viewModel.Id}";
    }
}