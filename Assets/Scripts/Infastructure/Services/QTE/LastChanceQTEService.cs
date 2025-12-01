using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Infastructure.Services.PlayerInput;
using Infastructure.Services.VolumeManagement;
using Infastructure.StaticData.LastChance;
using Infastructure.StaticData.StaticDataService;
using Zenject;

namespace Infastructure.Services.QTE
{
    public class LastChanceQTEService : ILastChanceQTEService, ITickable
    {
        private readonly IInputService _inputService;
        private readonly IStaticDataService _staticDataService;
        private readonly IVolumeService _volumeService;
        private LastChanceStaticData lastChanceStaticData => _staticDataService.LastChanceStaticData;

        private LastChanceUI _lastChanceUI;

        private CancellationTokenSource _cts;
        private bool _isRunning;
        private bool _canPress;


        public LastChanceQTEService(IInputService inputService, IStaticDataService staticDataService,
            IVolumeService volumeService)
        {
            _volumeService = volumeService;
            _staticDataService = staticDataService;
            _inputService = inputService;
        }

        public void Initialize(LastChanceUI lastChanceUI) =>
            _lastChanceUI = lastChanceUI;

        public void Tick()
        {
            if (!_isRunning)
                return;

            if (_canPress && _inputService.RightMousePressed)
                Save();
            else if (_inputService.AnyActionPressed)
                Lose().Forget();
        }

        public void StartQTE()
        {
            _volumeService.SetSaturation(-100);

            _cts = new CancellationTokenSource();

            _isRunning = true;
            _lastChanceUI.Show(() => WaitPressTimeAsync().Forget());
        }


        private async UniTask WaitPressTimeAsync()
        {
            if (_isRunning == false)
                return;

            _lastChanceUI.ChangeSelectedSprite();

            _canPress = true;

            await UniTask.Delay(TimeSpan.FromSeconds(lastChanceStaticData.PressWaitTime),
                cancellationToken: _cts.Token, delayType: DelayType.UnscaledDeltaTime);

            _lastChanceUI.ChangeDeSelectedSprite();

            Lose().Forget();
        }

        private async UniTask Lose()
        {
            Clear();

            await _lastChanceUI.ShakeIcon().AsyncWaitForCompletion();

            _lastChanceUI.Clear();

            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }

        private void Save()
        {
            Clear();

            _lastChanceUI.PickUpFlower();
            _lastChanceUI.Clear();

            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }

        private void Clear()
        {
            _volumeService.SetSaturation(0);

            _isRunning = false;
            _canPress = false;
        }
    }
}