using System.Collections.Generic;
using Infastructure.PlatformRegistry;
using Infastructure.Services.Magnet;
using Infastructure.Services.PlayerInput;
using Infastructure.Services.PlayerInput.InputSourceRealization;
using Infastructure.StaticData.StaticDataService;
using SpiderController.StateMachine;
using UnityEngine;

namespace SpiderController.Platform
{
    public class PlatformSelector
    {
        private readonly IPlatformRegistryService _platformRegistryService;

        private readonly Material _planeBlinkMaterial;
        private readonly int _excludeLayers =
            1 << LayerMask.NameToLayer("Product") | 1 << LayerMask.NameToLayer("Flower");

        private readonly Dictionary<int, PlatformId> _platformIds = new Dictionary<int, PlatformId>
        {
            { 0, PlatformId.Circle },
            { 1, PlatformId.Box },
            { 2, PlatformId.Surf },
        };

        private readonly StateMachineData _stateMachineData;
        private readonly IInputService _inputService;
        private readonly IMagnetFreezingService _magnetFreezingService;

        private Material _defaultMaterial;
        private Material _emissionMaterial;

        private bool _isBlinking;
        private float _blinkSpeed;
        private bool _IsOnPlatform;

        private Collider _productColliderCached;
        private JoystickInputSource _joystick;

        private int _currentIndex = 0;
        private bool _magnetIsActive;

        public PlatformSelector(
            StateMachineData stateMachineData,
            IStaticDataService staticDataService,
            IPlatformRegistryService platformRegistryService,
            IInputService inputService,
            IMagnetFreezingService magnetFreezingService)
        {
            _magnetFreezingService = magnetFreezingService;
            _stateMachineData = stateMachineData;
            _inputService = inputService;
            _platformRegistryService = platformRegistryService;

            _planeBlinkMaterial = new Material(staticDataService.MaterialsStaticData.PlaneBlinkMaterial);
            _defaultMaterial = new Material(staticDataService.MaterialsStaticData.LitDefaultMaterial);
        }

        public void Initialize()
        {
            _inputService.OnJoystickEnableHappend += JoystickEnabled;
            _inputService.OnJoystickDisableHappend += JoystickDisabled;

            _magnetFreezingService.OnFreezActiveChanged += SetEmissionMaterial;

            InitializePlatform(_platformIds[_currentIndex]);
        }

        public void Destroy()
        {
            _inputService.OnJoystickEnableHappend -= JoystickEnabled;
            _inputService.OnJoystickDisableHappend -= JoystickDisabled;

            _magnetFreezingService.OnFreezActiveChanged -= SetEmissionMaterial;
        }

        private void JoystickDisabled() =>
            _joystick = null;

        private void JoystickEnabled(IInputSource obj) =>
            _joystick = (JoystickInputSource)obj;

        public void Update()
        {
            KeyboardInput();
            JoystickInput();

            if (_IsOnPlatform && !_magnetFreezingService.IsActive)
                ChangeMaterial();
        }


        public void ReturnToDefaultMaterial()
        {
            if (!_isBlinking)
                return;

            _isBlinking = false;

            _platformRegistryService.CurrentPlatformData.MeshRenderer.material = _defaultMaterial;
        }

        public bool IsOnPlatform(Collider productCollider)
        {
            Collider platformCollider = _platformRegistryService.CurrentPlatformData.Collider;

            _productColliderCached = productCollider;
            _IsOnPlatform = IsInside(platformCollider, _productColliderCached);

            return _IsOnPlatform;
        }

        public void SetExcludeLayerMask()
        {
            _platformRegistryService.CurrentPlatformData.Collider.excludeLayers = _excludeLayers;
        }

        public void ResetExcludeLayerMask()
        {
            _IsOnPlatform = false;

            _platformRegistryService.CurrentPlatformData.Collider.excludeLayers = 0;
        }


        private void SetEmissionMaterial(bool isActive)
        {
            _magnetIsActive = isActive;

            _platformRegistryService.CurrentPlatformData.MeshRenderer.material =
                isActive ? _emissionMaterial : _defaultMaterial;
        }

        public bool IsInsideOfBlinkPlace(Collider productCollider)
        {
            Collider blinkCollider = _platformRegistryService.CurrentPlatformData.BlinkDetectionCollider;

            return IsInside(productCollider, blinkCollider);
        }


        private void KeyboardInput()
        {
            for (int i = 0; i < _platformIds.Count; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    if (_currentIndex == i)
                        break;

                    _currentIndex = i;
                    SelectPlatform(_platformIds[i]);
                    break;
                }
            }
        }

        private void JoystickInput()
        {
            if (_joystick != null)
            {
                if (_joystick.ChangePlatformLeftPressed && _currentIndex > 0)
                {
                    _currentIndex--;

                    SelectPlatform(_platformIds[_currentIndex]);
                }

                if (_joystick.ChangePlatformRightPressed && _currentIndex < _platformIds.Count - 1)
                {
                    _currentIndex++;

                    SelectPlatform(_platformIds[_currentIndex]);
                }
            }
        }


        private void InitializePlatform(PlatformId platformId)
        {
            _platformRegistryService.CurrentPlatformId = platformId;
            _stateMachineData.TotalWeight += _platformRegistryService.TryGetPlatformData(platformId).Weight;

            foreach (KeyValuePair<PlatformId, PlatformData> pair in _platformRegistryService.GetAllPlatforms())
            {
                PlatformId key = pair.Key;
                PlatformData platformData = pair.Value;

                if (key == platformId)
                {
                    Activate(platformData, true);

                    _emissionMaterial = platformData.MeshRenderer.material;
                    _platformRegistryService.CurrentPlatformData = platformData;

                    _platformRegistryService.CurrentPlatformData.MeshRenderer.material = _defaultMaterial;
                }
                else
                {
                    Activate(platformData, false);
                }
            }
        }

        private void SelectPlatform(PlatformId platformId)
        {
            PlatformData platformData = _platformRegistryService.TryGetPlatformData(platformId);
            if (platformData == null)
                return;

            Activate(platformData, true);
            Activate(_platformRegistryService.CurrentPlatformData, false);

            PlatformId lastId = _platformRegistryService.CurrentPlatformId;
            PlatformData lastPlatformData = _platformRegistryService.CurrentPlatformData;
            lastPlatformData.MeshRenderer.material = _emissionMaterial;

            _stateMachineData.TotalWeight -= _platformRegistryService.TryGetPlatformData(lastId).Weight;
            _stateMachineData.TotalWeight += _platformRegistryService.TryGetPlatformData(platformId).Weight;

            _platformRegistryService.CurrentPlatformData = platformData;
            _platformRegistryService.CurrentPlatformId = platformId;
            _emissionMaterial = platformData.MeshRenderer.material;

            SetEmissionMaterial(_magnetIsActive);

            if (_IsOnPlatform)
                SetExcludeLayerMask();
            else
                ResetExcludeLayerMask();
        }

        private void ChangeMaterial()
        {
            Collider blinkCollider = _platformRegistryService.CurrentPlatformData.BlinkDetectionCollider;

            bool isInside = IsInside(_productColliderCached, blinkCollider);

            if (isInside)
                ReturnToDefaultMaterial();
            else
                SetBlinkMaterial();
        }

        private void SetBlinkMaterial()
        {
            if (_isBlinking)
                return;

            _isBlinking = true;
            _platformRegistryService.CurrentPlatformData.MeshRenderer.material = _planeBlinkMaterial;
        }

        private void Activate(PlatformData platformData, bool value)
        {
            if (platformData == null)
                return;

            foreach (GameObject pieceObject in platformData.AllPieceObjects)
                pieceObject.SetActive(value);
        }

        private bool IsInside(Collider a, Collider b)
        {
            return Physics.ComputePenetration(
                a, a.transform.position, a.transform.rotation,
                b, b.transform.position, b.transform.rotation,
                out _, out _
            );
        }
    }
}