using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infastructure.Services.Ability;
using Infastructure.Services.PlatformObjects;
using Infastructure.Services.QTE;
using Infastructure.Services.StartGame;
using PickupObjects;
using PickupObjects.PickUpOnPlatform;
using SpiderController.StateMachine;
using Zenject;

namespace Infastructure.Services.Magnet
{
    public class MagnetFreezingService : IMagnetFreezingService, IInitializable, IDisposable
    {
        private readonly IPlatformObjectsService _platformObjectsService;
        private readonly IAbilityService _abilityService;
        private readonly ILastChanceQTEService _lastChanceQteService;
        private IStartGameReceiver _startGameReceiver;

        private StateMachineData _stateMachineData;

        public event Action<bool> OnFreezActiveChanged;

        public bool IsActive
        {
            get => _isActive;
            private set
            {
                bool newValue = _isActive;

                if (newValue != value)
                {
                    _isActive = value;

                    OnFreezActiveChanged?.Invoke(_isActive);
                }
            }
        }

        private bool _isActive;
        private bool _isSavingTime;

        public MagnetFreezingService(IPlatformObjectsService platformObjectsService, IAbilityService abilityService,
            ILastChanceQTEService lastChanceQteService, IStartGameReceiver startGameReceiver)
        {
            _startGameReceiver = startGameReceiver;
            _abilityService = abilityService;
            _lastChanceQteService = lastChanceQteService;
            _platformObjectsService = platformObjectsService;
        }

        public void Initialize()
        {
            _startGameReceiver.OnStartGameHappened += WaitTimeWithMagnet;
            _lastChanceQteService.OnSaveHappened += WaitTimeWithMagnet;
        }

        public void Dispose()
        {
            _startGameReceiver.OnStartGameHappened -= WaitTimeWithMagnet;
            _lastChanceQteService.OnSaveHappened -= WaitTimeWithMagnet;
        }

        public void Initialize(StateMachineData stateMachineData) =>
            _stateMachineData = stateMachineData;

        public void Freeze()
        {
            if (!_abilityService.IsExploredAbility(ProductType.MagnetSkillProduct) || _isSavingTime)
                return;

            foreach (PickupObjectBase pickupObject in _platformObjectsService.PickupObjects)
                pickupObject.IsFreezingOnPlatform = true;

            IsActive = true;
        }

        public void Unfreeze()
        {
            if (!_abilityService.IsExploredAbility(ProductType.MagnetSkillProduct) || _isSavingTime)
                return;

            if (_stateMachineData.IsMouseHolding && _stateMachineData.CurrentEnergyFillAmount > 0)
                return;

            foreach (PickupObjectBase pickupObject in _platformObjectsService.PickupObjects)
                pickupObject.IsFreezingOnPlatform = false;

            IsActive = false;
        }

        private void WaitTimeWithMagnet() =>
            WaitTimeWithMagnetAsync().Forget();

        private async UniTask WaitTimeWithMagnetAsync()
        {
            Freeze();
            _isSavingTime = true;

            await UniTask.Delay(TimeSpan.FromSeconds(2));

            _isSavingTime = false;
            Unfreeze();
        }
    }
}