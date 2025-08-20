using HUD;
using Infastructure.Common;
using Infastructure.Services.CheckPoint;
using Infastructure.Services.PlayerInput;
using Infastructure.States;
using Infastructure.StaticData.StaticDataService;
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
        [SerializeField] private Flower _flower;

        [SerializeField] private Transform _rotationPlaneTransform;
        [SerializeField] private LegDataStruct[] _legs;
        [SerializeField] private GroundChecker _groundChecker;
        public Rigidbody Rigidbody => _rigidbody;
        public GroundChecker GroundChecker => _groundChecker;
        public SpiderUI SpiderUI => _spiderUI;

        private Rigidbody _rigidbody;
        private SpiderStateMachine _spiderStateMachine;
        private SpiderPlane _spiderPlane;
        private FlowerPickup _flowerPickup;
        private CheckPointChanger _checkPointChanger;

        private HudUI _hudUI;


        private IInputService _inputService;
        private IStaticDataService _staticDataService;
        private IPickupDisplayer _pickupDisplayer;
        private IStateMachine _stateMachine1;
        private ICheckPointService _checkPointService;


        [Inject]
        public void Construct(IInputService inputService, IStaticDataService staticDataService,
            IPickupDisplayer pickupDisplayer, IStateMachine stateMachine, ICheckPointService checkPointService)
        {
            _checkPointService = checkPointService;
            _stateMachine1 = stateMachine;
            _pickupDisplayer = pickupDisplayer;
            _staticDataService = staticDataService;
            _inputService = inputService;
        }

        private void Awake() =>
            _rigidbody = GetComponent<Rigidbody>();

        private void OnDestroy() =>
            _spiderPlane.Destroy();

        public void Initialize(HudUI hudUI)
        {
            StateMachineData stateMachineData = new StateMachineData();

            _checkPointChanger = new CheckPointChanger(transform, _checkPointService);
            _spiderPlane = new SpiderPlane(_spiderUI.PlaneIndicatorUI, _rotationPlaneTransform, _inputService,
                _staticDataService, stateMachineData);
            _spiderPlane.Initialize();

            _spiderStateMachine = new SpiderStateMachine(this, stateMachineData, _inputService, _staticDataService,
                _legs,
                _flower);
            _flowerPickup = new FlowerPickup(_inputService, _pickupDisplayer, _flowerChecker, _flower);
            _spiderUI.StickerUI.PlaySticker(StickerEnum.StartGame);

            hudUI.RegisterFlowerPoint(_flower.transform);
            hudUI.RegisterFinishTarget(_checkPointService.PointIndicator);

            _flower.Initialize(hudUI.FlowerPointIndicator);
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
            _checkPointChanger.Update();
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