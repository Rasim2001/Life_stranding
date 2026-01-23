using System;
using System.Collections.Generic;
using Common;
using HighlightPlus;
using Infastructure.Common.Pickup;
using Infastructure.Common.StableWorlUpManagement;
using Infastructure.Data;
using Infastructure.PlatformRegistry;
using Infastructure.Services.Ability;
using Infastructure.Services.CameraProvider;
using Infastructure.Services.CheckPoint;
using Infastructure.Services.CutScene;
using Infastructure.Services.Defeat;
using Infastructure.Services.GravityGun;
using Infastructure.Services.Hint;
using Infastructure.Services.Magnet;
using Infastructure.Services.Pause;
using Infastructure.Services.PlatformObjects;
using Infastructure.Services.PlayerInput;
using Infastructure.Services.PlayerProgressService;
using Infastructure.Services.ProgressWatchers;
using Infastructure.Services.SaveLoadService;
using Infastructure.Services.Window;
using Infastructure.Services.XRay;
using Infastructure.States;
using Infastructure.StaticData.GlobalWater;
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

        [SerializeField] private Transform _rotationPlaneTransform;
        [SerializeField] private GroundChecker _groundChecker;

        [SerializeField] private HighlightEffect[] _energyHighlightEffects;
        [SerializeField] private LegDataStruct[] _legs;
        [SerializeField] private Dictionary<PlatformId, PlatformData> _platformDatas;

        public IMagnetFreezingService MagnetFreezingService => _magnetFreezingService;
        public IEventSystemSelector EventSystemSelector => _eventSystemSelector;
        public IAbilityService AbilityService => _abilityService;
        public IPauseService PauseService => _pauseService;
        public ICutSceneService CutSceneService => _cutSceneService;
        public IWindowService WindowService => _windowService;
        public ICameraProviderService CameraProviderService => _cameraProviderService;
        public Rigidbody Rigidbody => _rigidbody;
        public GroundChecker GroundChecker => _groundChecker;
        public SpiderUI SpiderUI => _spiderUI;
        public SpiderImpactReceiver SpiderImpactReceiver => _spiderImpactReceiver;
        public Transform RotationPlaneTransform => _rotationPlaneTransform;
        public ThrusterSystem ThrusterSystem => _thrusterSystem;
        public ScannerAnimator ScannerAnimator => _scannerAnimator;
        public PlatformSelector PlatformSelector => _platformSelector;
        public ObserverTrigger WaterObserverTrigger => _waterObserverTrigger;
        public WaterStaticData WaterStaticData => _staticDataService.WaterStaticData;
        public StateMachineData StateMachineData => _stateMachineData;
        public Stickers Stickers => _stickers;


        [HideInEditorMode] public Action<float> OnShakeCameraHappened;

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

        private IInputService _inputService;
        private IStaticDataService _staticDataService;
        private IPickupDisplayer _pickupDisplayer;
        private ICutSceneService _cutSceneService;
        private IMagnetFreezingService _magnetFreezingService;
        private IPlatformObjectsService _platformObjectsService;
        private IXRayService _xRayService;
        private IPlatformRegistryService _platformRegistryService;
        private IWindowService _windowService;
        private IEventSystemSelector _eventSystemSelector;
        private IPauseService _pauseService;
        private IAbilityService _abilityService;
        private IDefeatWindowService _defeatWindowService;

        private StateMachineData _stateMachineData;
        private SpiderOverlayStateMachine _overlayStateMachine;
        private MagnetSkill _magnetSkill;

        private IHintReceiverService _hintReceiverService;
        private ICameraProviderService _cameraProviderService;
        private IStableWorldUp _stableWorldUp;
        private IProgressWatchersService _progressWatchersService;
        private IPersistentProgressService _persistentProgressService;
        private IGravityGunDisplayer _gravityGunDisplayer;


        [Inject]
        public void Construct(
            IInputService inputService,
            IStaticDataService staticDataService,
            IPickupDisplayer pickupDisplayer,
            IStateMachine stateMachine,
            IBiospherePointService biospherePointService,
            ICutSceneService cutSceneService,
            IMagnetFreezingService magnetFreezingService,
            IPlatformObjectsService platformObjectsService,
            IXRayService xRayService,
            IPlatformRegistryService platformRegistryService,
            IWindowService windowService,
            IEventSystemSelector eventSystemSelector,
            IPauseService pauseService,
            IAbilityService abilityService,
            IDefeatWindowService defeatWindowService,
            IHintReceiverService hintReceiverService,
            ICameraProviderService cameraProviderService,
            IStableWorldUp stableWorldUp,
            IProgressWatchersService progressWatchersService,
            IPersistentProgressService persistentProgressService,
            IGravityGunDisplayer gravityGunDisplayer)
        {
            _gravityGunDisplayer = gravityGunDisplayer;
            _persistentProgressService = persistentProgressService;
            _progressWatchersService = progressWatchersService;
            _stableWorldUp = stableWorldUp;
            _cameraProviderService = cameraProviderService;
            _hintReceiverService = hintReceiverService;
            _defeatWindowService = defeatWindowService;
            _abilityService = abilityService;
            _pauseService = pauseService;
            _eventSystemSelector = eventSystemSelector;
            _windowService = windowService;
            _platformRegistryService = platformRegistryService;
            _xRayService = xRayService;
            _platformObjectsService = platformObjectsService;
            _magnetFreezingService = magnetFreezingService;
            _cutSceneService = cutSceneService;
            _pickupDisplayer = pickupDisplayer;
            _staticDataService = staticDataService;
            _inputService = inputService;
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
            _platformRegistryService.Register(_platformDatas);

            _stateMachineData = new StateMachineData();
            _stateMachineData.EnergyFillAmount = _staticDataService.SpiderStaticData.EnergyFillAmount;
            _stateMachineData.OnShakeHappened += distanceFalling => OnShakeCameraHappened?.Invoke(distanceFalling);

            _magnetFreezingService.Initialize(_stateMachineData);

            EnergyLegs energyLegs = new EnergyLegs(_energyHighlightEffects);

            EnergySystem energySystem = new EnergySystem(_stateMachineData, _spiderUI.EnergyBar, _cutSceneService);

            _spiderImpactReceiver = new SpiderImpactReceiver(_stateMachineData, transform);

            _spiderPlane = new SpiderPlane(_spiderUI.PlaneIndicatorUI, _rotationPlaneTransform, _inputService,
                _abilityService, _staticDataService, _windowService, _cameraProviderService, _stableWorldUp,
                _stateMachineData, _trajectoryRender);
            _spiderPlane.Initialize();

            _flowerPickup = new FlowerPickup(_inputService, _pickupDisplayer, _platformObjectsService, _windowService,
                _defeatWindowService, _cutSceneService, _flowerChecker, flower, _spiderUI,
                _staticDataService.SpiderStaticData);
            _flowerPickup.Initialize();

            _batteryProductPickup = new BatteryProductPickup(_hintReceiverService, _inputService, _pickupDisplayer,
                _platformObjectsService, _batteryChecker, _flowerChecker);
            _batteryProductPickup.Initialize();

            _energyPickup = new EnergyPickup(_persistentProgressService, _inputService, _pickupDisplayer, _xRayService,
                _windowService, _energyChecker, SpiderUI.EnergyBar, _stateMachineData, energyLegs);
            _energyPickup.Initialize();

            _elephantProductPickup = new ElephantProductPickup(_inputService, _pickupDisplayer, _platformObjectsService,
                _platformRegistryService, _elephantChecker);
            _elephantProductPickup.Initialize();

            _skillProductPickup = new SkillProductPickup(_inputService, _pickupDisplayer, _xRayService, _windowService,
                _skillChecker);
            _skillProductPickup.Initialize();

            _platformSelector = new PlatformSelector(_stateMachineData, _staticDataService, _platformRegistryService,
                _inputService, _magnetFreezingService);
            _platformSelector.Initialize();

            _checkpointPickup = new CheckpointPickup(_hintReceiverService, _inputService, _pickupDisplayer,
                _windowService, _platformObjectsService, _checkpointChecker, flower, _spiderUI);
            _checkpointPickup.Initialize();

            _generatorPickup = new GeneratorPickup(_hintReceiverService, _inputService, _pickupDisplayer,
                _platformObjectsService, _windowService, _cutSceneService, _generatorChecker);
            _generatorPickup.Initialize();

            _biosphereProductPickup =
                new BiosphereProductPickup(_inputService, _pickupDisplayer, _biosphereChecker, flower);
            _biosphereProductPickup.Initialize();

            _magnetSkill = new MagnetSkill(_windowService, _inputService, _staticDataService, _abilityService,
                _magnetFreezingService, _stateMachineData, energySystem, _spiderUI);
            _magnetSkill.Initialize();

            _spiderStateMachine =
                new SpiderStateMachine(this,
                    _stateMachineData,
                    _inputService,
                    _staticDataService,
                    _cutSceneService,
                    _platformObjectsService,
                    _legs,
                    flower,
                    energySystem);

            _overlayStateMachine =
                new SpiderOverlayStateMachine(_inputService, _gravityGunDisplayer, _cameraProviderService,
                    _staticDataService, _stateMachineData, _rotationPlaneTransform);

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
            Rigidbody.linearVelocity = Vector3.zero;
            Rigidbody.angularVelocity = Vector3.zero;

            _stateMachineData.Clear();
        }

        private void FixedUpdate()
        {
            if (_spiderStateMachine == null || _pauseService.IsPaused || _defeatWindowService.IsDefeated)
                return;

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