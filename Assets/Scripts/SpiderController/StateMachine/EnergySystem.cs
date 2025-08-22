using Infastructure.Services.CutScene;
using Infastructure.StaticData.Spider;
using Infastructure.StaticData.StaticDataService;
using SpiderController.UI;
using UnityEngine;

namespace SpiderController.StateMachine
{
    public class EnergySystem
    {
        private SpiderStaticData SpiderStaticData => _staticDataService.SpiderStaticData;

        private readonly StateMachineData _data;
        private readonly EnergyBarUI _energyBar;
        private readonly Spider _spider;
        private readonly IStaticDataService _staticDataService;
        private readonly ICutSceneService _cutSceneService;

        public EnergySystem(
            StateMachineData data,
            EnergyBarUI EnergyBar,
            IStaticDataService staticDataService,
            ICutSceneService cutSceneService)
        {
            _staticDataService = staticDataService;
            _cutSceneService = cutSceneService;
            _data = data;
            _energyBar = EnergyBar;
        }

        public void SpendEnergy(float speed)
        {
            if (_cutSceneService.IsActive)
                return;

            if (_data.EnergyFillAmount >= 0)
            {
                _data.EnergyFillAmount -= Time.deltaTime * speed /
                                          SpiderStaticData.EnergyFillAmount;

                _energyBar.SetEnergyValue(_data.EnergyFillAmount);
            }
        }

        public void RestoreEnergy(float speed)
        {
            if (_data.EnergyFillAmount < 1)
            {
                _data.EnergyFillAmount += Time.deltaTime * speed /
                                          SpiderStaticData.EnergyFillAmount;

                _energyBar.SetEnergyValue(_data.EnergyFillAmount);
            }
        }
    }
}