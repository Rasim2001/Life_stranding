using System.Collections.Generic;
using PickupObjects;

namespace Infastructure.Services.Restart
{
    public class RestartService : IRestartService
    {
        public bool IsRestarting { get; private set; }
        public List<ProductType> ExploredProducts { get; private set; }

        public void Restart(List<ProductType> exploredProducts)
        {
            //ExploredProducts = new List<ProductType>(exploredProducts);

            IsRestarting = true;
        }

        public void Clear() =>
            IsRestarting = false;
    }
}