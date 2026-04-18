using UnityEngine;

namespace Infastructure.Services.Teleports
{
    public interface ITeleportDisplayer
    {
        Ray GetAimRay(Camera mainCamera);
        void Show();
        void Hide();
    }
}