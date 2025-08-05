using _2;
using Infastructure.Common;
using Infastructure.Services.Input;
using Infastructure.StaticData.StaticDataService;
using SpiderController.SpiderMove;
using SpiderController.StateMachine;
using UnityEngine;
using Zenject;

namespace SpiderController
{
    [RequireComponent(typeof(Rigidbody))]
    public class Spider : MonoBehaviour
    {
        [SerializeField] private FlowerChecker _flowerChecker;
        [SerializeField] private Flower _flower;
        [SerializeField] private EnergyUI _energyUI;
        [SerializeField] private PlaneIndicator _planeIndicator;
        [SerializeField] private Transform _rotationPlaneTransform;
        [SerializeField] private LegDataStruct[] _legs;
        [SerializeField] private GroundChecker _groundChecker;
        public Rigidbody Rigidbody => _rigidbody;
        public GroundChecker GroundChecker => _groundChecker;
        public EnergyUI EnergyUI => _energyUI;

        private Rigidbody _rigidbody;
        private SpiderStateMachine _stateMachine;
        private SpiderPlane _spiderPlane;
        private FlowerPickup _flowerPickup;

        private IInputService _inputService;
        private IStaticDataService _staticDataService;
        private IPickupDisplayer _pickupDisplayer;

        [Inject]
        public void Construct(IInputService inputService, IStaticDataService staticDataService,
            IPickupDisplayer pickupDisplayer)
        {
            _pickupDisplayer = pickupDisplayer;
            _staticDataService = staticDataService;
            _inputService = inputService;
        }

        private void Awake() =>
            _rigidbody = GetComponent<Rigidbody>();

        public void Initialize()
        {
            _spiderPlane = new SpiderPlane(_planeIndicator, _rotationPlaneTransform, _inputService, _staticDataService);
            _stateMachine = new SpiderStateMachine(this, _inputService, _staticDataService, _legs, _flower);
            _flowerPickup = new FlowerPickup(_inputService, _pickupDisplayer, _flowerChecker, _flower);
        }

        private void Update()
        {
            if (_stateMachine == null)
                return;

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