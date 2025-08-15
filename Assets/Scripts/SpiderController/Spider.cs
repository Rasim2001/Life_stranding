using System;
using HUD;
using Infastructure.Common;
using Infastructure.Services.Input;
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
        private SpiderStateMachine _stateMachine;
        private SpiderPlane _spiderPlane;
        private FlowerPickup _flowerPickup;
        private HudUI _hudUI;

        private IInputService _inputService;
        private IStaticDataService _staticDataService;
        private IPickupDisplayer _pickupDisplayer;
        private IStateMachine _stateMachine1;


        [Inject]
        public void Construct(IInputService inputService, IStaticDataService staticDataService,
            IPickupDisplayer pickupDisplayer, IStateMachine stateMachine)
        {
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

            _spiderPlane = new SpiderPlane(_spiderUI.PlaneIndicatorUI, _rotationPlaneTransform, _inputService,
                _staticDataService, stateMachineData);
            _spiderPlane.Initialize();

            _stateMachine = new SpiderStateMachine(this, stateMachineData, _inputService, _staticDataService, _legs,
                _flower);
            _flowerPickup = new FlowerPickup(_inputService, _pickupDisplayer, _flowerChecker, _flower);
            _spiderUI.StickerUI.PlaySticker(StickerEnum.StartGame);

            Vector3 finishTargetPosition = _staticDataService.GameStaticData.FinishTargetPosition;
            hudUI.RegisterFlowerPoint(_flower.transform);
            hudUI.RegisterFinishTarget(finishTargetPosition);

            _flower.Initialize(hudUI.FlowerPointIndicator);
        }


        private void Update()
        {
            if (_stateMachine == null)
                return;

            if (Input.GetKeyDown(KeyCode.Escape))
                _stateMachine1.Enter<LoadLevelState>(); //TODO:

            _stateMachine.HandleInput();
            _stateMachine.Update();
            _spiderPlane.Update();
            _flowerPickup.Update();
        }

        private void FixedUpdate()
        {
            if (_stateMachine == null)
                return;

            _stateMachine.FixedUpdate();
            _spiderPlane.FixedUpdate();
        }

        private void LateUpdate()
        {
            if (_stateMachine == null)
                return;

            _stateMachine.LateUpdate();
        }
    }
}