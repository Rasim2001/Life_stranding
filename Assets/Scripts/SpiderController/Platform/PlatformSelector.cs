using System.Collections.Generic;
using Infastructure.StaticData.StaticDataService;
using UnityEngine;

namespace SpiderController.Platform
{
    public class PlatformSelector
    {
        private Dictionary<PlatformId, PlatformData> _platformDatas;

        private readonly Material _planeBlinkMaterial;
        private Material _defaultMaterial;

        private bool _isBlinking;
        private float _blinkSpeed;
        private bool _IsOnPlatform;

        private PlatformData _currentPlatformData;
        private Vector3 _flowerPositionCached;

        public PlatformSelector(IStaticDataService staticDataService) =>
            _planeBlinkMaterial = new Material(staticDataService.MaterialsStaticData.PlaneBlinkMaterial);

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

            if (_IsOnPlatform)
                ChangeRobotPlaneColor(_flowerPositionCached);
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

        public bool IsOnPlatform(Vector3 flowerPosition)
        {
            _flowerPositionCached = flowerPosition;
            _IsOnPlatform = _currentPlatformData.Collider.bounds.Contains(flowerPosition);
            return _IsOnPlatform;
        }

        public void ReturnToDefaultMaterial()
        {
            if (!_isBlinking)
                return;

            _isBlinking = false;
            _currentPlatformData.MeshRenderer.material = _defaultMaterial;
        }

        private void ChangeRobotPlaneColor(Vector3 point)
        {
            Bounds bounds = _currentPlatformData.Collider.bounds;

            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;

            Vector3 offset = point - center;
            Vector3 absOffset = new Vector3(Mathf.Abs(offset.x), 0, Mathf.Abs(offset.z));

            Vector3 edgeDistance = extents - absOffset;
            float minDistance = Mathf.Min(Mathf.Abs(edgeDistance.x), Mathf.Abs(edgeDistance.z));

            if (minDistance < 0.3f)
                SetBlinkMaterial();
            else
                ReturnToDefaultMaterial();
        }

        private void Activate(PlatformData platformData, bool value)
        {
            if (platformData == null)
                return;

            foreach (GameObject pieceObject in platformData.AllPieceObjects)
                pieceObject.SetActive(value);

            platformData.Collider.enabled = value;
        }


        private void SetBlinkMaterial()
        {
            if (_isBlinking)
                return;

            _isBlinking = true;
            _currentPlatformData.MeshRenderer.material = _planeBlinkMaterial;
        }
    }
}