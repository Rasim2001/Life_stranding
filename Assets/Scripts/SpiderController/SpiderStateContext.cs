using Common;
using SpiderController.Scanner;
using SpiderController.SpiderMove;
using SpiderController.StateMachine;
using SpiderController.Thruster;
using SpiderController.Trajectory;
using SpiderController.TriggerChecker;
using SpiderController.UI.Health;
using SpiderController.UI.Stickers;
using UnityEngine;

namespace SpiderController
{
    public class SpiderStateContext
    {
        public Transform Transform { get; }
        public Transform RotationPlaneTransform { get; }
        public Rigidbody Rigidbody { get; }
        public SpiderUI SpiderUI { get; }
        public ThrusterSystem ThrusterSystem { get; }
        public ScannerAnimator ScannerAnimator { get; }
        public FlowerChecker FlowerChecker { get; }
        public BatteryProductChecker BatteryChecker { get; }
        public EnergyChecker EnergyChecker { get; }
        public ElephantChecker ElephantChecker { get; }
        public SkillProductChecker SkillChecker { get; }
        public CheckpointChecker CheckpointChecker { get; }
        public GeneratorChecker GeneratorChecker { get; }
        public BiosphereChecker BiosphereChecker { get; }
        public ObserverTrigger WaterObserverTrigger { get; }
        public TrajectoryRender TrajectoryRender { get; }
        public Stickers Stickers { get; }
        public GroundChecker GroundChecker { get; }
        public StateMachineData Data { get; }
        public LegDataStruct[] Legs { get; }

        public SpiderStateContext(
            Transform transform,
            Transform rotationPlaneTransform,
            Rigidbody rigidbody,
            SpiderUI spiderUI,
            ThrusterSystem thrusterSystem,
            ScannerAnimator scannerAnimator,
            FlowerChecker flowerChecker,
            BatteryProductChecker batteryChecker,
            EnergyChecker energyChecker,
            ElephantChecker elephantChecker,
            SkillProductChecker skillChecker,
            CheckpointChecker checkpointChecker,
            GeneratorChecker generatorChecker,
            BiosphereChecker biosphereChecker,
            ObserverTrigger waterObserverTrigger,
            TrajectoryRender trajectoryRender,
            Stickers stickers,
            GroundChecker groundChecker,
            StateMachineData data,
            LegDataStruct[] legs)
        {
            RotationPlaneTransform = rotationPlaneTransform;
            Rigidbody = rigidbody;
            Transform = transform;
            SpiderUI = spiderUI;
            ThrusterSystem = thrusterSystem;
            ScannerAnimator = scannerAnimator;
            FlowerChecker = flowerChecker;
            BatteryChecker = batteryChecker;
            EnergyChecker = energyChecker;
            ElephantChecker = elephantChecker;
            SkillChecker = skillChecker;
            CheckpointChecker = checkpointChecker;
            GeneratorChecker = generatorChecker;
            BiosphereChecker = biosphereChecker;
            WaterObserverTrigger = waterObserverTrigger;
            TrajectoryRender = trajectoryRender;
            Stickers = stickers;
            GroundChecker = groundChecker;
            Data = data;
            Legs = legs;
        }
        
        
    }
}