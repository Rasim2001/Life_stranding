using HUD;
using Infastructure.Common.Pickup;
using Infastructure.Services.CheckPoint;
using Infastructure.Services.CutScene;
using Infastructure.Services.Magnet;
using Infastructure.Services.PlayerInput;
using Infastructure.States;
using Infastructure.StaticData.StaticDataService;
using PickupObjects;
using SpiderController.SpiderMove;
using SpiderController.StateMachine;
using SpiderController.UI.Health;
using SpiderController.UI.Stickers;
using UnityEngine;
using Zenject;

namespace SpiderController
{
    [RequireComponent(typeof(Rigidbody))]
    public class Spider : MonoBehaviour
    {
        [SerializeField] private SpiderUI _spiderUI;
        [SerializeField] private FlowerChecker _flowerChecker;
        [SerializeField] private BatteryProductChecker _batteryChecker;
        [SerializeField] private MeshRenderer _boundPlaneMeshRender;

        [SerializeField] private Transform _rotationPlaneTransform;
        [SerializeField] private LegDataStruct[] _legs;
        [SerializeField] private GroundChecker _groundChecker;
        public Rigidbody Rigidbody => _rigidbody;
        public GroundChecker GroundChecker => _groundChecker;
        public SpiderUI SpiderUI => _spiderUI;
        public SpiderImpactReceiver SpiderImpactReceiver => _spiderImpactReceiver;
        public Transform RotationPlaneTransform => _rotationPlaneTransform;
        public MeshRenderer BoundPlaneMeshRender => _boundPlaneMeshRender;
        public IMagnetFreezingService MagnetFreezingService => _magnetFreezingService;

        private Rigidbody _rigidbody;
        private SpiderStateMachine _spiderStateMachine;
        private SpiderPlane _spiderPlane;
        private FlowerPickup _flowerPickup;
        private BatteryProductPickup _batteryProductPickup;
        private CheckPointChanger _checkPointChanger;
        private SpiderImpactReceiver _spiderImpactReceiver;

        private HudUI _hudUI;


        private IInputService _inputService;
        private IStaticDataService _staticDataService;
        private IPickupDisplayer _pickupDisplayer;
        private IStateMachine _stateMachine1;
        private ICheckPointService _checkPointService;
        private ICutSceneService _cutSceneService;
        private IMagnetFreezingService _magnetFreezingService;


        [Inject]
        public void Construct(IInputService inputService, IStaticDataService staticDataService,
            IPickupDisplayer pickupDisplayer, IStateMachine stateMachine, ICheckPointService checkPointService,
            ICutSceneService cutSceneService, IMagnetFreezingService magnetFreezingService)
        {
            _magnetFreezingService = magnetFreezingService;
            _cutSceneService = cutSceneService;
            _checkPointService = checkPointService;
            _stateMachine1 = stateMachine;
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
        }

        public void Initialize(Flower flower)
        {
            StateMachineData stateMachineData = new StateMachineData();
            EnergySystem energySystem = new EnergySystem(stateMachineData, _spiderUI.EnergyBar, _staticDataService,
                _cutSceneService);

            _spiderImpactReceiver = new SpiderImpactReceiver(stateMachineData, transform);

            _checkPointChanger = new CheckPointChanger(transform, _checkPointService);
            _spiderPlane = new SpiderPlane(_spiderUI.PlaneIndicatorUI, _rotationPlaneTransform, _inputService,
                _staticDataService, stateMachineData);
            _spiderPlane.Initialize();

            _flowerPickup = new FlowerPickup(_inputService, _pickupDisplayer, _flowerChecker, flower);
            _batteryProductPickup = new BatteryProductPickup(_inputService, _pickupDisplayer, _batteryChecker);
            _batteryProductPickup.Initialize();

            _spiderUI.StickerUI.PlaySticker(StickerEnum.StartGame);

            _spiderStateMachine =
                new SpiderStateMachine(this,
                    stateMachineData,
                    _inputService,
                    _staticDataService,
                    _legs,
                    flower,
                    energySystem);
        }


        private void Update()
        {
            if (_spiderStateMachine == null)
                return;

            if (Input.GetKeyDown(KeyCode.Escape))
                _stateMachine1.Enter<LoadLevelState>(); //TODO:

            _spiderStateMachine.HandleInput();
            _spiderStateMachine.Update();
            _spiderPlane.Update();
            _flowerPickup.Update();
            _batteryProductPickup.Update();
            _checkPointChanger.Update();
            _spiderImpactReceiver.Update();
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