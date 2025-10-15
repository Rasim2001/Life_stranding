using System;
using System.Collections.Generic;
using Common.SceneMarkers;
using HighlightPlus;
using HUD;
using Infastructure.Common.Pickup;
using Infastructure.Services.CheckPoint;
using Infastructure.Services.CutScene;
using Infastructure.Services.Magnet;
using Infastructure.Services.PlatformObjects;
using Infastructure.Services.PlayerInput;
using Infastructure.Services.XRay;
using Infastructure.States;
using Infastructure.StaticData.StaticDataService;
using PickupObjects.PickUpOnPlatform;
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

        [SerializeField] private Transform _rotationPlaneTransform;
        [SerializeField] private GroundChecker _groundChecker;

        [SerializeField] private HighlightEffect[] _energyHighlightEffects;
        [SerializeField] private LegDataStruct[] _legs;
        [SerializeField] private Dictionary<PlatformId, PlatformData> _platformDatas;

        public IMagnetFreezingService MagnetFreezingService => _magnetFreezingService;
        public Rigidbody Rigidbody => _rigidbody;
        public GroundChecker GroundChecker => _groundChecker;
        public SpiderUI SpiderUI => _spiderUI;
        public SpiderImpactReceiver SpiderImpactReceiver => _spiderImpactReceiver;
        public Transform RotationPlaneTransform => _rotationPlaneTransform;
        public ThrusterSystem ThrusterSystem => _thrusterSystem;
        public ScannerAnimator ScannerAnimator => _scannerAnimator;
        public PlatformSelector PlatformSelector => _platformSelector;

        [HideInEditorMode] public Action<float> OnShakeCameraHappened;

        private Rigidbody _rigidbody;
        private SpiderStateMachine _spiderStateMachine;
        private SpiderPlane _spiderPlane;
        private CheckPointChanger _checkPointChanger;
        private SpiderImpactReceiver _spiderImpactReceiver;
        private PlatformSelector _platformSelector;

        private FlowerPickup _flowerPickup;
        private BatteryProductPickup _batteryProductPickup;
        private EnergyPickup _energyPickup;
        private ElephantProductPickup _elephantProductPickup;

        private HudUI _hudUI;

        private IInputService _inputService;
        private IStaticDataService _staticDataService;
        private IPickupDisplayer _pickupDisplayer;
        private IStateMachine _stateMachine;
        private ICheckPointService _checkPointService;
        private ICutSceneService _cutSceneService;
        private IMagnetFreezingService _magnetFreezingService;
        private IPlatformObjectsService _platformObjectsService;
        private IXRayService _xRayService;


        [Inject]
        public void Construct(
            IInputService inputService,
            IStaticDataService staticDataService,
            IPickupDisplayer pickupDisplayer,
            IStateMachine stateMachine,
            ICheckPointService checkPointService,
            ICutSceneService cutSceneService,
            IMagnetFreezingService magnetFreezingService,
            IPlatformObjectsService platformObjectsService,
            IXRayService xRayService)
        {
            _xRayService = xRayService;
            _platformObjectsService = platformObjectsService;
            _magnetFreezingService = magnetFreezingService;
            _cutSceneService = cutSceneService;
            _checkPointService = checkPointService;
            _stateMachine = stateMachine;
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
        }

        public void Initialize(Flower flower)
        {
            EnergyLegs energyLegs = new EnergyLegs(_energyHighlightEffects);

            StateMachineData stateMachineData = new StateMachineData();
            stateMachineData.EnergyFillAmount = _staticDataService.SpiderStaticData.EnergyFillAmount;
            stateMachineData.OnShakeHappened += distanceFalling => OnShakeCameraHappened?.Invoke(distanceFalling);


            EnergySystem energySystem = new EnergySystem(stateMachineData, _spiderUI.EnergyBar, _cutSceneService);

            _spiderImpactReceiver = new SpiderImpactReceiver(stateMachineData, transform);

            _checkPointChanger = new CheckPointChanger(transform, _checkPointService);
            _spiderPlane = new SpiderPlane(_spiderUI.PlaneIndicatorUI, _rotationPlaneTransform, _inputService,
                _staticDataService, stateMachineData);
            _spiderPlane.Initialize();

            _flowerPickup = new FlowerPickup(_inputService, _pickupDisplayer, _platformObjectsService, _flowerChecker,
                flower, _spiderUI.HealthBar);
            _flowerPickup.Initialize();

            _batteryProductPickup = new BatteryProductPickup(_inputService, _pickupDisplayer, _platformObjectsService,
                _batteryChecker, _flowerChecker);
            _batteryProductPickup.Initialize();

            _energyPickup = new EnergyPickup(_inputService, _pickupDisplayer, _xRayService, _energyChecker,
                SpiderUI.EnergyBar, stateMachineData, energyLegs);
            _energyPickup.Initialize();

            _elephantProductPickup = new ElephantProductPickup(_inputService, _pickupDisplayer, _platformObjectsService,
                _elephantChecker);
            _elephantProductPickup.Initialize();

            _spiderUI.StickerUI.PlaySticker(StickerEnum.StartGame);


            _spiderStateMachine =
                new SpiderStateMachine(this,
                    stateMachineData,
                    _inputService,
                    _staticDataService,
                    _legs,
                    flower,
                    energySystem);

            _platformSelector = new PlatformSelector(_staticDataService, _spiderStateMachine);
            _platformSelector.Initialize(_platformDatas);
        }


        private void Update()
        {
            if (_spiderStateMachine == null)
                return;

            if (Input.GetKeyDown(KeyCode.Escape))
                _stateMachine.Enter<LoadLevelState>(); //TODO:

            _spiderStateMachine.HandleInput();
            _spiderStateMachine.Update();
            _spiderPlane.Update();
            _flowerPickup.Update();
            _batteryProductPickup.Update();
            _checkPointChanger.Update();
            _spiderImpactReceiver.Update();
            _energyPickup.Update();
            _platformSelector.Update();
            _elephantProductPickup.Update();
        }

        private void FixedUpdate()
        {
            if (_spiderStateMachine == null)
                return;

            _spiderStateMachine.FixedUpdate();
            _spiderPlane.FixedUpdate();
        }

        private void LateUpdate()
        {
            if (_spiderStateMachine == null)
                return;

            _spiderStateMachine.LateUpdate();
        }
    }
}