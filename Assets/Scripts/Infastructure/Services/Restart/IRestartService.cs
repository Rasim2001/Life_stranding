using System.Collections.Generic;
using PickupObjects;

namespace Infastructure.Services.Restart
{
    public interface IRestartService
    {
        bool IsRestarting { get; }
        List<ProductType> ExploredProducts { get; }
        void Restart(List<ProductType> exploredProducts);
        void Clear();
    }
}