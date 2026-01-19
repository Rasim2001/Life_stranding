using UnityEngine;

namespace Infastructure.Common.Trajectory
{
    public interface ITrajectoryEndPointDisplayer
    {
        void Show();
        void Hide();
        void Apply(RaycastHit hit);
    }
}