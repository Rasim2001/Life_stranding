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
        private readonly SpiderStateMachine _spiderStateMachine;

        private readonly Material _planeBlinkMaterial;
        private Material _defaultMaterial;

        private bool _isBlinking;
        private float _blinkSpeed;
        private bool _IsOnPlatform;

        private Vector3 _pointPositionCached;

        public PlatformSelector(
            IStaticDataService staticDataService,
            IPlatformRegistryService platformRegistryService,
            SpiderStateMachine spiderStateMachine)
        {
            _platformRegistryService = platformRegistryService;
            _spiderStateMachine = spiderStateMachine;
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
            
            if (_IsOnPlatform && _spiderStateMachine.IsCurrentState<AirbornState>() == false)
                ChangeMaterial();
        }

        public void ReturnToDefaultMaterial()
        {
            if (!_isBlinking)
                return;

            _isBlinking = false;
            _platformRegistryService.CurrentPlatformData.MeshRenderer.material = _defaultMaterial;
        }

        public bool IsOnPlatform(Vector3 flowerPosition)
        {
            _pointPositionCached = flowerPosition;
            _IsOnPlatform = _platformRegistryService.CurrentPlatformData.Collider.bounds.Contains(flowerPosition);

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
            Vector3 closestPoint =
                _platformRegistryService.CurrentPlatformData.BlinkDetectionCollider.ClosestPoint(_pointPositionCached);
            closestPoint.y = 0;
            _pointPositionCached.y = 0;

            bool isInside = (closestPoint - _pointPositionCached).sqrMagnitude < Mathf.Epsilon;

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
    }
}