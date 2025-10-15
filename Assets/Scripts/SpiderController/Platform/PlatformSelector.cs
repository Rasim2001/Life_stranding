using System.Collections.Generic;
using Infastructure.StaticData.StaticDataService;
using SpiderController.StateMachine;
using SpiderController.StateMachine.States.Airborn;
using UnityEngine;

namespace SpiderController.Platform
{
    public class PlatformSelector
    {
        private readonly SpiderStateMachine _spiderStateMachine;
        private Dictionary<PlatformId, PlatformData> _platformDatas;

        private readonly Material _planeBlinkMaterial;
        private Material _defaultMaterial;

        private bool _isBlinking;
        private float _blinkSpeed;
        private bool _IsOnPlatform;

        private PlatformData _currentPlatformData;
        private Vector3 _pointPositionCached;

        public PlatformSelector(IStaticDataService staticDataService, SpiderStateMachine spiderStateMachine)
        {
            _spiderStateMachine = spiderStateMachine;
            _planeBlinkMaterial = new Material(staticDataService.MaterialsStaticData.PlaneBlinkMaterial);
        }

        public void Initialize(Dictionary<PlatformId, PlatformData> platformDatas)
        {
            _platformDatas = new Dictionary<PlatformId, PlatformData>(platformDatas);

            InitializePlatform(PlatformId.Circle);
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
                SelectPlatform(PlatformId.Circle);

            if (Input.GetKeyDown(KeyCode.Alpha2))
                SelectPlatform(PlatformId.Box);

            if (_IsOnPlatform && _spiderStateMachine.IsCurrentState<AirbornState>() == false)
                ChangeMaterial();
        }

        public void ReturnToDefaultMaterial()
        {
            if (!_isBlinking)
                return;

            _isBlinking = false;
            _currentPlatformData.MeshRenderer.material = _defaultMaterial;
        }

        public bool IsOnPlatform(Vector3 flowerPosition)
        {
            _pointPositionCached = flowerPosition;
            _IsOnPlatform = _currentPlatformData.Collider.bounds.Contains(flowerPosition);

            return _IsOnPlatform;
        }

        private void InitializePlatform(PlatformId platformId)
        {
            foreach (KeyValuePair<PlatformId, PlatformData> pair in _platformDatas)
            {
                PlatformId key = pair.Key;
                PlatformData platformData = pair.Value;

                if (key == platformId)
                {
                    Activate(platformData, true);

                    _defaultMaterial = platformData.MeshRenderer.material;
                    _currentPlatformData = platformData;
                }
                else
                {
                    Activate(platformData, false);
                }
            }
        }

        private void SelectPlatform(PlatformId platformId)
        {
            if (!_platformDatas.TryGetValue(platformId, out PlatformData platformData))
                return;

            Activate(platformData, true);
            Activate(_currentPlatformData, false);

            _defaultMaterial = platformData.MeshRenderer.material;
            _currentPlatformData = platformData;
        }

        private void SetBlinkMaterial()
        {
            if (_isBlinking)
                return;

            _isBlinking = true;
            _currentPlatformData.MeshRenderer.material = _planeBlinkMaterial;
        }

        private void ChangeMaterial()
        {
            Vector3 closestPoint = _currentPlatformData.BlinkDetectionCollider.ClosestPoint(_pointPositionCached);
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

            platformData.Collider.enabled = value;
            platformData.BlinkDetectionCollider.enabled = value;
        }
    }
}