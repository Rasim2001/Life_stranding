using System.Collections.Generic;
using Infastructure.PlatformRegistry;
using Infastructure.StaticData.StaticDataService;
using SpiderController.StateMachine;
using SpiderController.StateMachine.States.Airborn;
using UnityEngine;

namespace SpiderController.Platform
{
    public class PlatformSelector
    {
        private readonly IPlatformRegistryService _platformRegistryService;

        private readonly Material _planeBlinkMaterial;
        private Material _defaultMaterial;

        private bool _isBlinking;
        private float _blinkSpeed;
        private bool _IsOnPlatform;

        private Collider _productColliderCached;

        public PlatformSelector(
            IStaticDataService staticDataService,
            IPlatformRegistryService platformRegistryService)
        {
            _platformRegistryService = platformRegistryService;
            _planeBlinkMaterial = new Material(staticDataService.MaterialsStaticData.PlaneBlinkMaterial);
        }

        public void Initialize() =>
            InitializePlatform(PlatformId.Circle);

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
                SelectPlatform(PlatformId.Circle);

            if (Input.GetKeyDown(KeyCode.Alpha2))
                SelectPlatform(PlatformId.Box);

            if (Input.GetKeyDown(KeyCode.Alpha3))
                SelectPlatform(PlatformId.Surf);

            if (_IsOnPlatform)
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

        private void InitializePlatform(PlatformId platformId)
        {
            _platformRegistryService.CurrentPlatformId = platformId;

            foreach (KeyValuePair<PlatformId, PlatformData> pair in _platformRegistryService.GetAllPlatforms())
            {
                PlatformId key = pair.Key;
                PlatformData platformData = pair.Value;

                if (key == platformId)
                {
                    Activate(platformData, true);

                    _defaultMaterial = platformData.MeshRenderer.material;
                    _platformRegistryService.CurrentPlatformData = platformData;
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

            _platformRegistryService.CurrentPlatformData = platformData;
            _platformRegistryService.CurrentPlatformId = platformId;
            _defaultMaterial = platformData.MeshRenderer.material;
        }

        private void SetBlinkMaterial()
        {
            if (_isBlinking)
                return;

            _isBlinking = true;
            _platformRegistryService.CurrentPlatformData.MeshRenderer.material = _planeBlinkMaterial;
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