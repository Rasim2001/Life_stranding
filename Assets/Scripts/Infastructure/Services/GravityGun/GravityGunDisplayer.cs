using UnityEngine;

namespace Infastructure.Services.GravityGun
{
    public class GravityGunDisplayer : MonoBehaviour, IGravityGunDisplayer
    {
        [SerializeField] private GameObject _aimObject;

        private void Start() =>
            Hide();

        public void Show() =>
            _aimObject.SetActive(true);

        public void Hide() =>
            _aimObject.SetActive(false);
    }
}