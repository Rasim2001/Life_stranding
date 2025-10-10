using Infastructure.Services.CutScene;
using Infastructure.StaticData.Spider;
using Infastructure.StaticData.StaticDataService;
using SpiderController.UI;
using UnityEngine;

namespace SpiderController.StateMachine
{
    public class EnergySystem
    {
        private readonly StateMachineData _data;
        private readonly EnergyBarUI _energyBar;
        private readonly Spider _spider;
        private readonly ICutSceneService _cutSceneService;

        public EnergySystem(
            StateMachineData data,
            EnergyBarUI EnergyBar,
            ICutSceneService cutSceneService)
        {
            _cutSceneService = cutSceneService;
            _data = data;
            _energyBar = EnergyBar;
        }

        public void SpendEnergy(float speed)
        {
            if (_cutSceneService.IsActive)
                return;

            if (_data.CurrentEnergyFillAmount >= 0)
            {
                _data.CurrentEnergyFillAmount -= Time.deltaTime * speed /
                                          _data.EnergyFillAmount;

                _energyBar.SetValue(_data.CurrentEnergyFillAmount);
            }
        }

        public void RestoreEnergy(float speed)
        {
            if (_data.CurrentEnergyFillAmount < 1)
            {
                _data.CurrentEnergyFillAmount += Time.deltaTime * speed /
                                          _data.EnergyFillAmount;

                _energyBar.SetValue(_data.CurrentEnergyFillAmount);
            }
        }
    }
}