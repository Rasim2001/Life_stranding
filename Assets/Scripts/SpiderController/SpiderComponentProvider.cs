using System;
using System.Collections.Generic;
using Common;
using SpiderController.Scanner;
using SpiderController.Thruster;
using SpiderController.Trajectory;
using SpiderController.TriggerChecker;
using SpiderController.UI.Health;
using SpiderController.UI.Stickers;
using UnityEngine;

namespace SpiderController
{
    public class SpiderComponentProvider : ComponentProviderBase
    {
        [SerializeField] private SpiderUI _spiderUI;
        [SerializeField] private ThrusterSystem _thrusterSystem;
        [SerializeField] private ScannerAnimator _scannerAnimator;
        [SerializeField] private FlowerChecker _flowerChecker;
        [SerializeField] private BatteryProductChecker _batteryChecker;
        [SerializeField] private EnergyChecker _energyChecker;
        [SerializeField] private ElephantChecker _elephantChecker;
        [SerializeField] private SkillProductChecker _skillChecker;
        [SerializeField] private CheckpointChecker _checkpointChecker;
        [SerializeField] private GeneratorChecker _generatorChecker;
        [SerializeField] private BiosphereChecker _biosphereChecker;
        [SerializeField] private ObserverTrigger _waterObserverTrigger;
        [SerializeField] private TrajectoryRender _trajectoryRender;
        [SerializeField] private Stickers _stickers;
        [SerializeField] private GroundChecker _groundChecker;

        protected override void RegisterMap(Dictionary<Type, object> map)
        {
            map.Add(typeof(SpiderUI), _spiderUI);
            map.Add(typeof(ThrusterSystem), _thrusterSystem);
            map.Add(typeof(ScannerAnimator), _scannerAnimator);
            map.Add(typeof(FlowerChecker), _flowerChecker);
            map.Add(typeof(BatteryProductChecker), _batteryChecker);
            map.Add(typeof(EnergyChecker), _energyChecker);
            map.Add(typeof(ElephantChecker), _elephantChecker);
            map.Add(typeof(SkillProductChecker), _skillChecker);
            map.Add(typeof(CheckpointChecker), _checkpointChecker);
            map.Add(typeof(GeneratorChecker), _generatorChecker);
            map.Add(typeof(BiosphereChecker), _biosphereChecker);
            map.Add(typeof(ObserverTrigger), _waterObserverTrigger);
            map.Add(typeof(TrajectoryRender), _trajectoryRender);
            map.Add(typeof(Stickers), _stickers);
            map.Add(typeof(GroundChecker), _groundChecker);
        }
    }
}