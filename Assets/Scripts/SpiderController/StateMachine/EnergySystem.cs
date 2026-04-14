using Infastructure.Services.CutScene;
using Infastructure.StaticData.Spider;
using Infastructure.StaticData.StaticDataService;
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


        public EnergySystem(
            SpiderStateContext stateContext,
            ICutSceneService cutSceneService)
        {
            _stateContext = stateContext;
            _cutSceneService = cutSceneService;
        }

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
            if (Data.CurrentEnergyFillAmount < 1)
            {
                Data.CurrentEnergyFillAmount += Time.deltaTime * speed /
                                                Data.EnergyFillAmount;

                EnergyBar.SetValue(Data.CurrentEnergyFillAmount);
            }
        }
    }
}