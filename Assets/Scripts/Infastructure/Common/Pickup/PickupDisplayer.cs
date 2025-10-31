using System.Collections.Generic;
using HighlightPlus;
using Infastructure.Services.Pool;
using Infastructure.Services.XRay;
using Infastructure.StaticData.XRay;
using UnityEngine;
using Zenject;

namespace Infastructure.Common.Pickup
{
    public class PickupDisplayer : MonoBehaviour, IPickupDisplayer
    {
        private readonly Dictionary<int, PickupView> _pickups = new Dictionary<int, PickupView>();

        private IPoolObjects<PickupView> _poolObjects;
        private IXRayService _xRayService;

        [Inject]
        public void Construct(IPoolObjects<PickupView> poolObjects, IXRayService xRayService)
        {
            _xRayService = xRayService;
            _poolObjects = poolObjects;
        }


        public void Show(Transform pickupTarget)
        {
            int id = pickupTarget.GetInstanceID();

            if (_pickups.ContainsKey(id))
                return;

            PickupView pickupView = _poolObjects.GetObjectFromPool();
            pickupView.transform.SetParent(transform);
            pickupView.Initialize(pickupTarget);

            HighlightEffect highlightEffect = pickupTarget.GetComponent<HighlightEffect>();
            highlightEffect?.SetHighlighted(true);

            if (pickupTarget.TryGetComponent<XRayMarker>(out var xRayMarker))
                _xRayService.Show(xRayMarker);

            _pickups.Add(id, pickupView);
        }

        public void Hide(Transform pickupTarget)
        {
            int id = pickupTarget.GetInstanceID();

            if (!_pickups.TryGetValue(id, out PickupView pickupView))
                return;

            HighlightEffect highlightEffect = pickupTarget.GetComponent<HighlightEffect>();
            highlightEffect?.SetHighlighted(false);

            if (pickupTarget.TryGetComponent<XRayMarker>(out var xRayMarker))
                _xRayService.Hide(xRayMarker);

            _poolObjects.ReturnObjectToPool(pickupView);
            _pickups.Remove(id);
        }
        
    }
}