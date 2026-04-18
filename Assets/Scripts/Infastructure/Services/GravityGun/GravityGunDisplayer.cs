using UnityEngine;

namespace Infastructure.Services.GravityGun
{
    public class GravityGunDisplayer : MonoBehaviour, IGravityGunDisplayer
    {
        [SerializeField] private GameObject _aimObject;
        
        private RectTransform _rectTransform;

        private void Awake() => 
            _rectTransform = _aimObject.GetComponent<RectTransform>();

        private void Start() =>
            Hide();
        
        public Ray GetAimRay(Camera mainCamera)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, _rectTransform.position);
            return mainCamera.ScreenPointToRay(screenPoint);
        }

        public void Show() =>
            _aimObject.SetActive(true);

        public void Hide() =>
            _aimObject.SetActive(false);
    }
}