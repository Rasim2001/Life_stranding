using System.Collections.Generic;
using System.Linq;
using HighlightPlus;
using Infastructure.Services.Pool;
using UnityEngine;
using Zenject;

namespace Infastructure.Common.Pickup
{
    public class PickupDisplayer : MonoBehaviour, IPickupDisplayer
    {
        private readonly Dictionary<int, PickupView> _pickups = new Dictionary<int, PickupView>();

        private IPoolObjects<PickupView> _poolObjects;

        [Inject]
        public void Construct(IPoolObjects<PickupView> poolObjects) =>
            _poolObjects = poolObjects;


        public void Show(Transform pickupTarget)
        {
            int id = pickupTarget.GetInstanceID();

            if (_pickups.ContainsKey(id))
                return;

            PickupView pickupView = _poolObjects.GetObjectFromPool();
            pickupView.transform.SetParent(transform);
            pickupView.transform.position = pickupTarget.position + Vector3.up;

            HighlightEffect highlightEffect = pickupTarget.GetComponent<HighlightEffect>();
            highlightEffect?.SetHighlighted(true);

            _pickups.Add(id, pickupView);
        }

        public void Hide(Transform pickupTarget)
        {
            int id = pickupTarget.GetInstanceID();

            if (!_pickups.TryGetValue(id, out PickupView pickupView))
                return;

            HighlightEffect highlightEffect = pickupTarget.GetComponent<HighlightEffect>();
            highlightEffect?.SetHighlighted(false);

            _poolObjects.ReturnObjectToPool(pickupView);
            _pickups.Remove(id);
        }

        public void HideRemainingObjects(Collider[] allColliders)
        {
            IEnumerable<int> allActiveInstanceIds = allColliders
                .Where(x => x != null)
                .Select(x => x.GetInstanceID());

            List<int> toHide = _pickups.Keys.Except(allActiveInstanceIds).ToList();

            foreach (int id in toHide)
            {
                PickupView pickupView = _pickups[id];
                _poolObjects.ReturnObjectToPool(pickupView);
                _pickups.Remove(id);
            }
        }
    }
}