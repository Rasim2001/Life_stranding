using UnityEngine;

namespace Infastructure.Services.CheckPoint
{
    public interface ICheckPointService
    {
        void GoToNextPoint();
        Transform PointIndicator { get; set; }
    }
}