using Common.Lock;
using Infastructure.Services.CutScene;
using SpiderController.UI;
using UnityEngine;

namespace SpiderController.StateMachine
{
    public class EnergySystem
    {
        private readonly ICutSceneService _cutSceneService;
        private readonly SpiderStateContext _stateContext;
        private StateMachineData Data => _stateContext.Data;
        private EnergyBarUI EnergyBar => _stateContext.SpiderUI.EnergyBar;

        private readonly LockHandler _lockHandler = new LockHandler();

        private bool _isLocked;

        public EnergySystem(
            SpiderStateContext stateContext,
            ICutSceneService cutSceneService)
        {
            _stateContext = stateContext;
            _cutSceneService = cutSceneService;
        }

        public void LockRestore(object owner) =>
            _lockHandler.Acquire(owner, () => _isLocked = true);

        public void UnlockRestore(object owner) =>
            _lockHandler.Release(owner, () => _isLocked = false);

        public void SpendEnergy(float speed)
        {
            if (_cutSceneService.IsActive)
                return;

            if (Data.CurrentEnergyFillAmount >= 0)
            {
                Data.CurrentEnergyFillAmount -= Time.deltaTime * speed /
                                                Data.EnergyFillAmount;

                EnergyBar.SetValue(Data.CurrentEnergyFillAmount);
            }
        }

        public void RestoreEnergy(float speed)
        {
            if (_isLocked)
                return;

            if (Data.CurrentEnergyFillAmount < 1)
            {
                Data.CurrentEnergyFillAmount += Time.deltaTime * speed /
                                                Data.EnergyFillAmount;

                EnergyBar.SetValue(Data.CurrentEnergyFillAmount);
            }
        }
    }
}