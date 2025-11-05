using System;
using System.Collections.Generic;
using HighlightPlus;
using HUD;
using Infastructure.Common.Pickup;
using Infastructure.PlatformRegistry;
using Infastructure.Services.Ability;
using Infastructure.Services.CheckPoint;
using Infastructure.Services.CutScene;
using Infastructure.Services.Magnet;
using Infastructure.Services.Pause;
using Infastructure.Services.PlatformObjects;
using Infastructure.Services.PlayerInput;
using Infastructure.Services.Window;
using Infastructure.Services.XRay;
using Infastructure.States;
using Infastructure.StaticData.StaticDataService;
using PickupObjects.PickUpOnPlatform;
using PickupObjects.PickUpOnPlatform.FlowerManagement;
using Sirenix.OdinInspector;
using SpiderController.PickUp;
using SpiderController.Platform;
using SpiderController.Scanner;
using SpiderController.SpiderMove;
using SpiderController.StateMachine;
using SpiderController.Thruster;
using SpiderController.TriggerChecker;
using SpiderController.UI.Health;
using SpiderController.UI.Stickers;
using UnityEngine;
using Zenject;

namespace SpiderController
{
    [RequireComponent(typeof(Rigidbody))]
    public class Spider : SerializedMonoBehaviour
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
        [SerializeField] private Stickers _stickers;

        [SerializeField] private Transform _rotationPlaneTransform;
        [SerializeField] private GroundChecker _groundChecker;

        [SerializeField] private HighlightEffect[] _energyHighlightEffects;
        [SerializeField] private LegDataStruct[] _legs;
        [SerializeField] private Dictionary<PlatformId, PlatformData> _platformDatas;

        public IMagnetFreezingService MagnetFreezingService => _magnetFreezingService;
        public IEventSystemSelector EventSystemSelector => _eventSystemSelector;
        public IAbilityService AbilityService => _abilityService;
        public Rigidbody Rigidbody => _rigidbody;
        public GroundChecker GroundChecker => _groundChecker;
        public SpiderUI SpiderUI => _spiderUI;
        public SpiderImpactReceiver SpiderImpactReceiver => _spiderImpactReceiver;
        public Transform RotationPlaneTransform => _rotationPlaneTransform;
        public ThrusterSystem ThrusterSystem => _thrusterSystem;
        public ScannerAnimator ScannerAnimator => _scannerAnimator;
        public PlatformSelector PlatformSelector => _platformSelector;
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

        private HudUI _hudUI;

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
            IAbilityService abilityService)
        {
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

        private void Awake() =>
            _rigidbody = GetComponent<Rigidbody>();

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
        }

        public void Initialize(Flower flower)
        {
            _platformRegistryService.Register(_platformDatas);

            StateMachineData stateMachineData = new StateMachineData();
            stateMachineData.EnergyFillAmount = _staticDataService.SpiderStaticData.EnergyFillAmount;
            stateMachineData.OnShakeHappened += distanceFalling => OnShakeCameraHappened?.Invoke(distanceFalling);

            EnergyLegs energyLegs = new EnergyLegs(_energyHighlightEffects);

            EnergySystem energySystem = new EnergySystem(stateMachineData, _spiderUI.EnergyBar, _cutSceneService);

            _spiderImpactReceiver = new SpiderImpactReceiver(stateMachineData, transform);

            _spiderPlane = new SpiderPlane(_spiderUI.PlaneIndicatorUI, _rotationPlaneTransform, _inputService,
                _abilityService, _staticDataService, stateMachineData);
            _spiderPlane.Initialize();

            _flowerPickup = new FlowerPickup(_inputService, _pickupDisplayer, _platformObjectsService, _windowService,
                _flowerChecker, flower, _spiderUI, _staticDataService.SpiderStaticData);
            _flowerPickup.Initialize();

            _batteryProductPickup = new BatteryProductPickup(_inputService, _pickupDisplayer, _platformObjectsService,
                _batteryChecker, _flowerChecker);
            _batteryProductPickup.Initialize();

            _energyPickup = new EnergyPickup(_inputService, _pickupDisplayer, _xRayService, _windowService,
                _energyChecker, SpiderUI.EnergyBar, stateMachineData, energyLegs);
            _energyPickup.Initialize();

            _elephantProductPickup = new ElephantProductPickup(_inputService, _pickupDisplayer, _platformObjectsService,
                _platformRegistryService,
                _elephantChecker);
            _elephantProductPickup.Initialize();

            _skillProductPickup = new SkillProductPickup(_inputService, _pickupDisplayer, _xRayService, _windowService,
                _skillChecker);
            _skillProductPickup.Initialize();

            _platformSelector = new PlatformSelector(_staticDataService, _platformRegistryService);
            _platformSelector.Initialize();

            _checkpointPickup = new CheckpointPickup(_inputService, _pickupDisplayer, _windowService,
                _checkpointChecker, flower, _spiderUI);
            _checkpointPickup.Initialize();

            _generatorPickup = new GeneratorPickup(_inputService, _pickupDisplayer, _platformObjectsService,
                _generatorChecker);
            _generatorPickup.Initialize();

            _spiderStateMachine =
                new SpiderStateMachine(this,
                    stateMachineData,
                    _inputService,
                    _staticDataService,
                    _cutSceneService,
                    _legs,
                    flower,
                    energySystem);
        }


        public void ForceLegsAfterTeleport() //TODO:
        {
            foreach (LegDataStruct legDataStruct in _legs)
                legDataStruct.Raycast.ForceImmediateUpdate();
        }


        private void Update()
        {
            if (_spiderStateMachine == null || _pauseService.IsPaused)
                return;

            _spiderStateMachine.HandleInput();
            _spiderStateMachine.Update();
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
        }

        private void FixedUpdate()
        {
            if (_spiderStateMachine == null || _pauseService.IsPaused)
                return;

            _spiderStateMachine.FixedUpdate();
            _spiderPlane.FixedUpdate();
        }

        private void LateUpdate()
        {
            if (_spiderStateMachine == null || _pauseService.IsPaused)
                return;

            _spiderStateMachine.LateUpdate();
        }
    }
}