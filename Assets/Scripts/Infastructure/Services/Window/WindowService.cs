using System;
using Cysharp.Threading.Tasks;
using Infastructure.Services.Ability;
using Infastructure.Services.CutScene;
using Infastructure.Services.Restart;
using Infastructure.Services.TaskPopupChecker;
using Infastructure.StaticData.Product;
using Infastructure.StaticData.StaticDataService;
using Infastructure.StaticData.Task;
using PickupObjects;
using UI;
using UI.MVVM.View.DefeatPopup;
using UI.MVVM.View.ProductDescriptionPopup;
using UI.MVVM.View.Root;
using UI.MVVM.View.SettingsPopup;
using UI.MVVM.View.SettingsScreen;
using UI.MVVM.View.StartSplashScreen;
using UI.MVVM.View.TaskPopup;
using UI.MVVM.View.WinPopup;
using UnityEngine;
using Zenject;

namespace Infastructure.Services.Window
{
    public class WindowService : IWindowService, IInitializable, IDisposable
    {
        private readonly UIGameplayRootViewModel _gamePlayViewModel;
        private readonly IStaticDataService _staticDataService;
        private readonly IAbilityService _abilityService;
        private readonly ITaskPopupCheckerService _taskPopupCheckerService;
        private readonly ICutSceneService _cutSceneService;
        private readonly IRestartService _restartService;

        public WindowService(UIGameplayRootViewModel gamePlayViewModel, IStaticDataService staticDataService,
            IAbilityService abilityService, ITaskPopupCheckerService taskPopupCheckerService,
            ICutSceneService cutSceneService, IRestartService restartService)
        {
            _abilityService = abilityService;
            _taskPopupCheckerService = taskPopupCheckerService;
            _cutSceneService = cutSceneService;
            _restartService = restartService;
            _staticDataService = staticDataService;
            _gamePlayViewModel = gamePlayViewModel;
        }

        public void Initialize()
        {
            if (_restartService.IsRestarting)
                TryOpenMainTaskPopup(false);

            _cutSceneService.OnCutsceneActiveChanged += TryOpenMainTaskPopup;
        }

        public void Dispose() =>
            _cutSceneService.OnCutsceneActiveChanged -= TryOpenMainTaskPopup;

        public void OpenStartSplashScreen()
        {
            StartSplashScreenViewModel viewModel = new StartSplashScreenViewModel(this);

            _gamePlayViewModel.OpenScreen(viewModel);
        }

        public void OpenPausePopup()
        {
            PausePopupViewModel model = new PausePopupViewModel();

            _gamePlayViewModel.OpenPopup(model);
        }

        public void OpenSettingsScreen()
        {
            SettingsScreenViewModel model = new SettingsScreenViewModel(this);

            _gamePlayViewModel.OpenScreen(model);
        }

        public void OpenWinPopup()
        {
            WinPopupViewModel model = new WinPopupViewModel();

            _gamePlayViewModel.OpenPopup(model);
        }

        public void OpenDefeatPopup()
        {
            DefeatPopupViewModel model = new DefeatPopupViewModel();

            _gamePlayViewModel.OpenPopup(model);
        }

        public void OpenProductDescriptionPopup(ProductType productType)
        {
            if (_abilityService.IsExploredAbility(productType))
                return;

            _abilityService.PickUpAbility(productType);

            ProductData productData = _staticDataService.ProductsStaticData.ProductsDictionary[productType];

            ProductDescriptionPopupViewModel model =
                new ProductDescriptionPopupViewModel(productData.ProductDescription);

            _gamePlayViewModel.OpenPopup(model);
        }

        public void OpenTaskPopup(TaskId taskId)
        {
            if (_taskPopupCheckerService.IsWasOpened(taskId))
                return;

            _taskPopupCheckerService.AddTask(taskId);

            TaskData taskData = _staticDataService.TasksStaticData.TaskDatas[taskId];
            TaskPopupViewModel model = new TaskPopupViewModel(taskData);

            _gamePlayViewModel.OpenPopup(model);
        }

        public void ClosePopup(string id) =>
            _gamePlayViewModel.ClosePopup(id);

        public void TryOpenMainTaskPopup(bool isActive)
        {
            if (!isActive)
                OpenMainTaskPopupAsync().Forget();
        }

        private async UniTask OpenMainTaskPopupAsync()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(2.2f));

            OpenTaskPopup(TaskId.MainTask);
        }
    }
}