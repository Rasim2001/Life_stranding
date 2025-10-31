using System;
using System.Collections.Generic;
using HUD;
using Infastructure.Services.Pool;
using Infastructure.StaticData.StaticDataService;
using Infastructure.StaticData.XRay;
using UnityEngine;

namespace Infastructure.Services.XRay
{
    public class XRayService : IXRayService
    {
        private readonly Dictionary<string, XRayOccluderUI> _xRayDictionary =
            new Dictionary<string, XRayOccluderUI>();

        private readonly IPoolObjects<XRayOccluderUI> _pools;
        private readonly IStaticDataService _staticDataService;

        private Transform _xRayContainer;
        private Transform _hudTransform;

        public XRayService(IStaticDataService staticDataService, IPoolObjects<XRayOccluderUI> pools)
        {
            _staticDataService = staticDataService;
            _pools = pools;
        }

        public void Initialize(Transform xRayContainer, Transform hudTransform)
        {
            _hudTransform = hudTransform;
            _xRayContainer = xRayContainer;
        }

        public void Add(XRayMarker xRayMarker)
        {
            if (xRayMarker.Id != string.Empty)
                return;

            string id = Guid.NewGuid().ToString();
            xRayMarker.Id = id;

            Sprite sprite = _staticDataService.XRayCollectionStaticData.XRayObjects[xRayMarker.Type];

            XRayOccluderUI xRayOccluderUI = _pools.GetObjectFromPool();
            xRayOccluderUI.transform.SetParent(_xRayContainer);
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
    }
}