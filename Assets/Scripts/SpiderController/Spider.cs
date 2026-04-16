using System;
using System.Collections.Generic;
using Common;
using HighlightPlus;
using Infastructure.Data;
using Infastructure.Factories;
using Infastructure.PlatformRegistry;
using Infastructure.Services.Defeat;
using Infastructure.Services.Magnet;
using Infastructure.Services.Pause;
using Infastructure.Services.PlatformObjects;
using Infastructure.Services.ProgressWatchers;
using Infastructure.Services.SaveLoadService;
using Infastructure.StaticData.StaticDataService;
using PickupObjects.PickUpOnPlatform.FlowerManagement;
using Sirenix.OdinInspector;
using SpiderController.Magnet;
using SpiderController.PickUp;
using SpiderController.Platform;
using SpiderController.Scanner;
using SpiderController.SpiderMove;
using SpiderController.StateMachine;
using SpiderController.Thruster;
using SpiderController.Trajectory;
using SpiderController.TriggerChecker;
using SpiderController.UI.Health;
using SpiderController.UI.Stickers;
using UnityEngine;
using Zenject;

namespace SpiderController
{
    [RequireComponent(typeof(Rigidbody))]
    public class Spider : SerializedMonoBehaviour, ISavedProgress
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

        [SerializeField] private Transform _rotationPlaneTransform;

        [SerializeField] private HighlightEffect[] _energyHighlightEffects;
        [SerializeField] private LegDataStruct[] _legs;
        [SerializeField] private Dictionary<PlatformId, PlatformData> _platformDatas;

        public StateMachineData StateMachineData => _stateMachineData;

        private Rigidbody _rigidbody;
        private SpiderStateMachine _spiderStateMachine;
        private SpiderPlane _spiderPlane;
        private SpiderImpactReceiver _spiderImpactReceiver;
        private PlatformSelector _platformSelector;

        private FlowerPickup _flowerPickup;
        private BatteryProductPickup _batteryProductPickup;
        private EnergyPickup _energyPickup;
        private ElephantProductPickup _elephantProductPickup;
        private SkillProductPickup _skillProductPickup;
        private CheckpointPickup _checkpointPickup;
        private GeneratorPickup _generatorPickup;
        private BiosphereProductPickup _biosphereProductPickup;

        private StateMachineData _stateMachineData;
        private SpiderOverlayStateMachine _overlayStateMachine;
        private MagnetSkill _magnetSkill;

        private IMagnetFreezingService _magnetFreezingService;
        private IPlatformObjectsService _platformObjectsService;
        private IPlatformRegistryService _platformRegistryService;
        private IPauseService _pauseService;
        private IDefeatWindowService _defeatWindowService;
        private IProgressWatchersService _progressWatchersService;
        private IDiFactory _diFactory;
        private IStaticDataService _staticData;


        [Inject]
        public void Construct(
            IStaticDataService staticData,
            IDiFactory diFactory,
            IPlatformObjectsService platformObjectsService,
            IMagnetFreezingService magnetFreezingService,
            IPlatformRegistryService platformRegistryService,
            IPauseService pauseService,
            IDefeatWindowService defeatWindowService,
            IProgressWatchersService progressWatchersService)
        {
            _staticData = staticData;
            _diFactory = diFactory;
            _progressWatchersService = progressWatchersService;
            _defeatWindowService = defeatWindowService;
            _pauseService = pauseService;
            _platformRegistryService = platformRegistryService;
            _platformObjectsService = platformObjectsService;
            _magnetFreezingService = magnetFreezingService;
        }


        public void LoadProgress(PlayerProgress progress)
        {
            if (progress.WorldProgressData.SpiderData.Position == null)
                return;

            transform.position = progress.WorldProgressData.SpiderData.Position.AsUnityVector();
            transform.localEulerAngles = progress.WorldProgressData.SpiderData.Rotation.AsUnityVector();
        }

        public void UpdateProgress(PlayerProgress progress)
        {
            progress.WorldProgressData.SpiderData.Position = transform.position.AsVectorData();
            progress.WorldProgressData.SpiderData.Rotation = transform.localEulerAngles.AsVectorData();
        }

        private void Awake() =>
            _rigidbody = GetComponent<Rigidbody>();

        private void Start() =>
            _defeatWindowService.OnDefeatHappened += Defeat;

        private void OnDestroy()
        {
            _spiderPlane.Destroy();
            _batteryProductPickup.Destroy();
            _flowerPickup.Destroy();
            _energyPickup.Destroy();
            _elephantProductPickup.Destroy();
            _skillProductPickup.Destroy();
            _checkpointPickup.Destroy();
            _generatorPickup.Destroy();
            _platformSelector.Destroy();
            _biosphereProductPickup.Destroy();
            _magnetSkill.Destroy();

            _defeatWindowService.OnDefeatHappened -= Defeat;
        }

        public void Initialize(Flower flower)
        {
            SpiderServiceContext serviceContext = _diFactory.Create<SpiderServiceContext>();

            _platformRegistryService.Register(_platformDatas);

            _stateMachineData = new StateMachineData();
            _stateMachineData.EnergyFillAmount = _staticData.SpiderStaticData.EnergyFillAmount;

            SpiderStateContext stateContext = new SpiderStateContext(
                transform,
                _rotationPlaneTransform,
                _rigidbody,
                _spiderUI,
                _thrusterSystem,
                _scannerAnimator,
                _flowerChecker,
                _batteryChecker,
                _energyChecker,
                _elephantChecker,
                _skillChecker,
                _checkpointChecker,
                _generatorChecker,
                _biosphereChecker,
                _waterObserverTrigger,
                _trajectoryRender,
                _stickers,
                _groundChecker,
                _stateMachineData,
                _legs
            );


            EnergyLegs energyLegs = new EnergyLegs(_energyHighlightEffects);
            EnergySystem energySystem = _diFactory.Create<EnergySystem>(stateContext);

            _spiderImpactReceiver = _diFactory.Create<SpiderImpactReceiver>(stateContext);

            _spiderPlane = _diFactory.Create<SpiderPlane>(stateContext);
            _spiderPlane.Initialize();

            _flowerPickup = _diFactory.Create<FlowerPickup>(stateContext, flower);
            _flowerPickup.Initialize();

            _batteryProductPickup = _diFactory.Create<BatteryProductPickup>(_batteryChecker, _flowerChecker);
            _batteryProductPickup.Initialize();

            _energyPickup = _diFactory.Create<EnergyPickup>(stateContext, _energyChecker, energyLegs);
            _energyPickup.Initialize();

            _elephantProductPickup = _diFactory.Create<ElephantProductPickup>(_elephantChecker);
            _elephantProductPickup.Initialize();

            _skillProductPickup = _diFactory.Create<SkillProductPickup>(_skillChecker);
            _skillProductPickup.Initialize();

            _platformSelector = _diFactory.Create<PlatformSelector>(_stateMachineData);
            _platformSelector.Initialize();

            _checkpointPickup = _diFactory.Create<CheckpointPickup>(_checkpointChecker, flower, _spiderUI);
            _checkpointPickup.Initialize();

            _generatorPickup = _diFactory.Create<GeneratorPickup>(_generatorChecker);
            _generatorPickup.Initialize();

            _biosphereProductPickup = _diFactory.Create<BiosphereProductPickup>(_biosphereChecker, flower);
            _biosphereProductPickup.Initialize();

            _magnetSkill = _diFactory.Create<MagnetSkill>(stateContext, energySystem);
            _magnetSkill.Initialize();

            _spiderStateMachine = _diFactory.Create<SpiderStateMachine>(stateContext, serviceContext, energySystem);
            _overlayStateMachine = _diFactory.Create<SpiderOverlayStateMachine>(stateContext);

            _magnetFreezingService.Initialize(_stateMachineData);
            _platformObjectsService.Initialize(stateContext, _platformSelector);

            _progressWatchersService.RegisterWatcher(_energyPickup);
        }


        public void ForceLegsAfterTeleport() //TODO:
        {
            foreach (LegDataStruct legDataStruct in _legs)
                legDataStruct.Raycast.ForceImmediateUpdate();
        }


        private void Update()
        {
            if (_spiderStateMachine == null || _pauseService.IsPaused || _defeatWindowService.IsDefeated)
                return;

            _spiderStateMachine.HandleInput();
            _spiderStateMachine.Update();

            _platformObjectsService.Update();

            _overlayStateMachine.HandleInput();
            _overlayStateMachine.Update();

            _spiderPlane.Update();
            _flowerPickup.Update();
            _batteryProductPickup.Update();
            _spiderImpactReceiver.Update();
            _energyPickup.Update();
            _platformSelector.Update();
            _elephantProductPickup.Update();
            _skillProductPickup.Update();
            _checkpointPickup.Update();
            _generatorPickup.Update();
            _biosphereProductPickup.Update();
            _magnetSkill.Update();
        }

        private void Defeat()
        {
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;

            _stateMachineData.Clear();
        }

        private void FixedUpdate()
        {
            if (_spiderStateMachine == null || _pauseService.IsPaused || _defeatWindowService.IsDefeated)
                return;

            _platformObjectsService.FixedUpdate();

            _spiderStateMachine.FixedUpdate();
            _overlayStateMachine.FixedUpdate();
            _spiderPlane.FixedUpdate();
        }

        private void LateUpdate()
        {
            if (_spiderStateMachine == null || _pauseService.IsPaused || _defeatWindowService.IsDefeated)
                return;

            _spiderStateMachine.LateUpdate();
            _overlayStateMachine.LateUpdate();
        }
    }
}