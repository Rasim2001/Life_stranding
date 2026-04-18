using UnityEngine;

namespace Infastructure.Services.GravityGun
{
    public interface IGravityGunDisplayer
    {
        void Show();
        void Hide();
        Ray GetAimRay(Camera mainCamera);
    }
}