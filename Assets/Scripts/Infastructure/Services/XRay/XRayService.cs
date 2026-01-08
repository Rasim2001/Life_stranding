using System;
using System.Collections.Generic;
using GameDevBuddies;
using HUD;
using Infastructure.Services.CameraProvider;
using Infastructure.Services.Pool;
using Infastructure.Services.SpiderTrack;
using Infastructure.StaticData.StaticDataService;
using Infastructure.StaticData.XRay;
using UnityEngine;

namespace Infastructure.Services.XRay
{
    public class XRayService : IXRayService, IDisposable
    {
        private readonly Dictionary<string, XRayOccluderUI> _xRayDictionary =
            new Dictionary<string, XRayOccluderUI>();

        private readonly IPoolObjects<XRayOccluderUI> _pools;
        private readonly ISpiderTrackService _spiderTrackService;
        private readonly ICameraProviderService _cameraProviderService;
        private readonly IStaticDataService _staticDataService;

        private Transform _xRayContainer;
        private Transform _hudTransform;
        private Transform _containerDisabled;

        private TerrainScan _terrain;

        public XRayService(IStaticDataService staticDataService, IPoolObjects<XRayOccluderUI> pools,
            ISpiderTrackService spiderTrackService, ICameraProviderService cameraProviderService)
        {
            _staticDataService = staticDataService;
            _pools = pools;
            _spiderTrackService = spiderTrackService;
            _cameraProviderService = cameraProviderService;
        }

        public void Initialize(Transform xRayContainer, Transform hudTransform, Transform containerDisabled)
        {
            _hudTransform = hudTransform;
            _xRayContainer = xRayContainer;
            _containerDisabled = containerDisabled;
        }

        public void Initialize()
        {
            _terrain = TerrainScan.Instance;
            _terrain.OnTerrainScanStart += ScanStart;
        }

        public void Dispose()
        {
            if (_terrain != null)
                TerrainScan.Instance.OnTerrainScanStart -= ScanStart;
        }

        public void Add(XRayMarker xRayMarker)
        {
            if (xRayMarker.Id != string.Empty)
                return;

            string id = Guid.NewGuid().ToString();
            xRayMarker.Id = id;

            Sprite sprite = _staticDataService.XRayCollectionStaticData.XRayObjects[xRayMarker.Type];

            XRayOccluderUI xRayOccluderUI = _pools.GetObjectFromPool();
            xRayOccluderUI.transform.SetParent(_containerDisabled);
            xRayOccluderUI.Initialize(xRayMarker.transform, sprite);

            _xRayDictionary.Add(id, xRayOccluderUI);
        }

        public void Remove(XRayMarker xRayMarker)
        {
            string id = xRayMarker.Id;
            xRayMarker.Id = string.Empty;

            if (!_xRayDictionary.TryGetValue(id, out XRayOccluderUI xRayOccluderUI))
                return;

            _pools.ReturnObjectToPool(xRayOccluderUI);
            _xRayDictionary.Remove(id);
        }


        public void Show(XRayMarker xRayMarker)
        {
            if (!_xRayDictionary.TryGetValue(xRayMarker.Id, out XRayOccluderUI xRayOccluderUI))
                return;

            xRayOccluderUI.transform.SetParent(_hudTransform);
        }

        public void Hide(XRayMarker xRayMarker)
        {
            if (!_xRayDictionary.TryGetValue(xRayMarker.Id, out XRayOccluderUI xRayOccluderUI))
                return;

            xRayOccluderUI.transform.SetParent(_xRayContainer);
        }

        private void ScanStart(TerrainScanInfo obj)
        {
            foreach (XRayOccluderUI xRay in _xRayDictionary.Values)
            {
                float distance = Vector3.Distance(xRay.TargetWorldObject.position,
                    _spiderTrackService.Spider.transform.position);

                xRay.transform.SetParent(
                    distance < 50
                        ? _xRayContainer
                        : _containerDisabled);
            }
        }
    }
}