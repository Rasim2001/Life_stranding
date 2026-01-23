using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Infastructure.Services.Hint;
using Infastructure.Services.Pause;
using Infastructure.Services.PlayerInput;
using Infastructure.Services.SlowTime;
using Infastructure.Services.VolumeManagement;
using Infastructure.Services.Window;
using Infastructure.StaticData.LastChance;
using Infastructure.StaticData.StaticDataService;
using SpiderController.UI.LastChanceQTE;
using UnityEngine;
using Zenject;

namespace Infastructure.Services.QTE
{
    public class LastChanceQTEService : ILastChanceQTEService, ITickable
    {
        private readonly IInputService _inputService;
        private readonly IStaticDataService _staticDataService;
        private readonly IVolumeService _volumeService;
        private readonly IHintReceiverService _hintReceiverService;
        private readonly ISlowTimeRunner _slowTimeRunner;
        private readonly IPauseService _pauseService;
        private readonly IWindowService _windowService;

        public event Action OnSaveHappened;

        private LastChanceStaticData LastChanceStaticData => _staticDataService.LastChanceStaticData;

        private LastChanceRootUI _lastChanceRootUI;
        private LastChanceBarUI _lastChanceBarUI;

        private CancellationTokenSource _cts;
        private bool _isRunning;
        private bool _canPress;

        private int _allAttempts;
        private int _defaultAllAttempts;
        private float _allAttemptsFill;
        private int _currentAttempts;


        public LastChanceQTEService(IInputService inputService, IStaticDataService staticDataService,
            IVolumeService volumeService, IHintReceiverService hintReceiverService, ISlowTimeRunner slowTimeRunner,
            IPauseService pauseService, IWindowService windowService)
        {
            _pauseService = pauseService;
            _windowService = windowService;
            _slowTimeRunner = slowTimeRunner;
            _volumeService = volumeService;
            _hintReceiverService = hintReceiverService;
            _staticDataService = staticDataService;
            _inputService = inputService;

            _allAttempts = LastChanceStaticData.AllSaveAttempts;
            _allAttemptsFill = _allAttempts;
            _defaultAllAttempts = _allAttempts;
        }

        public void Initialize(LastChanceRootUI lastChanceRootUI, LastChanceBarUI lastChanceBarUI)
        {
            _lastChanceBarUI = lastChanceBarUI;
            _lastChanceRootUI = lastChanceRootUI;
        }

        public void Tick()
        {
            if (!_isRunning)
                return;

            if (_canPress && _inputService.RightMousePressed && !_windowService.IsOpenedAnyWindow())
                Save();
            else if (_inputService.AnyActionPressed && !IsSafeChance())
                Lose().Forget();
        }

        public void StartQTE()
        {
            if (_allAttempts <= 0)
            {
                _lastChanceBarUI.ShowHologram();
                _lastChanceBarUI.ShakeUI();
                _lastChanceBarUI.PlayFadeHologramEffect();
                return;
            }

            _allAttempts--;
            _currentAttempts++;

            UpdateLastChanceBar();

            if (IsSafeChance())
                _hintReceiverService.OnLastChanceHintShowHappened?.Invoke();

            _volumeService.SetSaturation(-100);

            _cts = new CancellationTokenSource();

            _isRunning = true;
            _lastChanceRootUI.Show(() => WaitPressTimeAsync().Forget());
        }

        private void UpdateLastChanceBar()
        {
            _lastChanceBarUI.UpdateSaveAttempts(_allAttempts);
            _lastChanceBarUI.ShowHologram();

            DOTween.To(() => _allAttemptsFill, x => _allAttemptsFill = x, _allAttempts, 1)
                .OnUpdate(() => _lastChanceBarUI.SetValue(_allAttemptsFill / _defaultAllAttempts));
        }


        private async UniTask WaitPressTimeAsync()
        {
            if (_isRunning == false)
                return;

            _lastChanceRootUI.ChangeSelectedSprite();

            _canPress = true;

            if (IsSafeChance())
            {
                _slowTimeRunner.StopSlowDown();
                _pauseService.StartPause(_lastChanceRootUI.name);
            }

            while (IsSafeChance())
                await UniTask.Yield(cancellationToken: _cts.Token);

            await UniTask.Delay(TimeSpan.FromSeconds(LastChanceStaticData.PressWaitTime),
                cancellationToken: _cts.Token);

            _lastChanceRootUI.ChangeDeSelectedSprite();

            Lose().Forget();
        }

        private async UniTask Lose()
        {
            Clear();

            await _lastChanceRootUI.ShakeIcon().AsyncWaitForCompletion();

            _lastChanceRootUI.Clear();

            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }

        private bool IsSafeChance() =>
            _currentAttempts <= LastChanceStaticData.SafeAttempts;

        private void Save()
        {
            Clear();

            if (IsSafeChance())
            {
                _pauseService.StopPause(_lastChanceRootUI.name);
                _hintReceiverService.OnLastChanceHintHideHappened?.Invoke();
            }

            _lastChanceRootUI.PickUpFlower();
            _lastChanceRootUI.Clear();

            _cts.Cancel();
            _cts.Dispose();
            _cts = null;

            OnSaveHappened?.Invoke();
        }

        private void Clear()
        {
            _volumeService.SetSaturation(0);

            _isRunning = false;
            _canPress = false;

            if (_allAttempts >= 0)
                _lastChanceBarUI.PlayFadeHologramEffect();
        }
    }
}