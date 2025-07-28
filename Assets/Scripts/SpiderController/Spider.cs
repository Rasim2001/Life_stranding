using _2;
using Infastructure.Services.Input;
using Infastructure.StaticData.StaticDataService;
using SpiderController.StateMachine;
using UnityEngine;
using Zenject;

namespace SpiderController
{
    [RequireComponent(typeof(Rigidbody))]
    public class Spider : MonoBehaviour
    {
        [SerializeField] private PlaneIndicator _planeIndicator;
        [SerializeField] private Transform _rotationPlaneTransform;
        [SerializeField] private LegDataStruct[] _legs;
        public Rigidbody Rigidbody => _rigidbody;

        private Rigidbody _rigidbody;
        private SpiderStateMachine _stateMachine;
        private SpiderPlane _spiderPlane;

        private IInputService _inputService;
        private IStaticDataService _staticDataService;

        [Inject]
        public void Construct(IInputService inputService, IStaticDataService staticDataService)
        {
            _staticDataService = staticDataService;
            _inputService = inputService;
        }

        private void Awake() =>
            _rigidbody = GetComponent<Rigidbody>();

        public void Initialize()
        {
            _spiderPlane = new SpiderPlane(_planeIndicator, _rotationPlaneTransform, _inputService, _staticDataService);
            _spiderPlane.Initialize();

            _stateMachine = new SpiderStateMachine(this, _inputService, _staticDataService, _legs);
        }

        private void Update()
        {
            if (_stateMachine == null)
                return;

            _stateMachine.HandleInput();
            _stateMachine.Update();
            _spiderPlane.Update();
        }

        private void FixedUpdate()
        {
            if (_stateMachine == null)
                return;

            _stateMachine.FixedUpdate();
            _spiderPlane.FixedUpdate();
        }
    }
}