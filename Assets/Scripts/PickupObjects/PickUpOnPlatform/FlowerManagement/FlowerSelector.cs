using Sirenix.Utilities;
using UnityEngine;

namespace PickupObjects.PickUpOnPlatform.FlowerManagement
{
    public class FlowerSelector
    {
        private readonly GameObject[] _variants;

        private Rigidbody[] _allRigidbody;
        private Collider[] _allColliders;
        private GameObject _currentVariant;
        private int _currentIndex;

        public FlowerSelector(GameObject[] variants) =>
            _variants = variants;

        public void Initialize()
        {
            _currentVariant = _variants[0];
            _currentVariant.gameObject.SetActive(true);

            _allRigidbody = _variants[^1].GetComponentsInChildren<Rigidbody>();
            _allColliders = _variants[^1].GetComponentsInChildren<Collider>();
        }

        public void ShowNextVariant()
        {
            if (IsLastVariant())
                CrushFlower();

            _currentIndex++;

            if (_variants.Length <= _currentIndex)
                return;

            _currentVariant.gameObject.SetActive(false);
            _currentVariant = _variants[_currentIndex];
            _currentVariant.gameObject.SetActive(true);
        }

        private bool IsLastVariant() =>
            _currentIndex == _variants.Length - 1;

        public void Clear()
        {
            _currentVariant = null;
            _currentIndex = 0;
        }


        private void CrushFlower()
        {
            _allRigidbody.ForEach(x => x.isKinematic = false);
            _allColliders.ForEach(x => x.enabled = true);
        }
    }
}