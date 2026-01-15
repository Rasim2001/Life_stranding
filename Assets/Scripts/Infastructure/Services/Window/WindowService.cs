using System;
using Cysharp.Threading.Tasks;
using Infastructure.Services.Ability;
using Infastructure.Services.CutScene;
using Infastructure.Services.Restart;
using Infastructure.Services.StartGame;
using Infastructure.Services.Tasks;
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
        public event Action OnWindowOpened;

        private readonly UIGameplayRootViewModel _gamePlayViewModel;
        private readonly IStaticDataService _staticDataService;
        private readonly IAbilityService _abilityService;
        private readonly ITasksService _tasksService;
        private readonly ICutSceneService _cutSceneService;
        private readonly IRestartService _restartService;
        private IStartGameReceiver _startGameReceiver;

        public WindowService(UIGameplayRootViewModel gamePlayViewModel, IStaticDataService staticDataService,
            IAbilityService abilityService, ITasksService tasksService,
            ICutSceneService cutSceneService, IRestartService restartService, IStartGameReceiver startGameReceiver)
        {
            _startGameReceiver = startGameReceiver;
            _abilityService = abilityService;
            _tasksService = tasksService;
            _cutSceneService = cutSceneService;
            _restartService = restartService;
            _staticDataService = staticDataService;
            _gamePlayViewModel = gamePlayViewModel;
        }

        public void Initialize() =>
            _startGameReceiver.OnStartGameHappened += OpenMainTaskPopup;

        public void Dispose() =>
            _startGameReceiver.OnStartGameHappened -= OpenMainTaskPopup;

        public void OpenStartSplashScreen()
        {
            StartSplashScreenViewModel viewModel = new StartSplashScreenViewModel(this);

            _gamePlayViewModel.OpenScreen(viewModel);
            OnWindowOpened?.Invoke();
        }

        public void OpenPausePopup()
        {
            PausePopupViewModel model = new PausePopupViewModel();

            _gamePlayViewModel.OpenPopup(model);
            OnWindowOpened?.Invoke();
        }

        public void OpenSettingsScreen()
        {
            SettingsScreenViewModel model = new SettingsScreenViewModel(this);

            _gamePlayViewModel.OpenScreen(model);
            OnWindowOpened?.Invoke();
        }

        public void OpenWinPopup()
        {
            WinPopupViewModel model = new WinPopupViewModel();

            _gamePlayViewModel.OpenPopup(model);
            OnWindowOpened?.Invoke();
        }

        public void OpenDefeatPopup()
        {
            DefeatPopupViewModel model = new DefeatPopupViewModel();

            _gamePlayViewModel.OpenPopup(model);
            OnWindowOpened?.Invoke();
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
            OnWindowOpened?.Invoke();
        }

        public void OpenTaskPopup(TaskId taskId)
        {
            if (_tasksService.IsWasOpened(taskId))
                return;

            _tasksService.AddTask(taskId);

            TaskData taskData = _staticDataService.TasksStaticData.TaskDatas[taskId];
            TaskPopupViewModel model = new TaskPopupViewModel(taskData);

            _gamePlayViewModel.OpenPopup(model);
            OnWindowOpened?.Invoke();
        }


        public void TryOpenMainTaskPopup(bool isActive)
        {
            if (!isActive)
                OpenMainTaskPopupAsync().Forget();
        }


        public void ClosePopup(string id) =>
            _gamePlayViewModel.ClosePopup(id);

        public bool CanOpenWindow() =>
            _gamePlayViewModel.CanOpenWindow();

        public bool IsOpenedAnyWindow() =>
            _gamePlayViewModel.IsOpenedAnyWindow();

        private void OpenMainTaskPopup() =>
            OpenMainTaskPopupAsync().Forget();


        private async UniTask OpenMainTaskPopupAsync()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(2f));

            OpenTaskPopup(TaskId.MainTask);
        }
    }
}