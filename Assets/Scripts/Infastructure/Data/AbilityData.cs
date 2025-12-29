using System;
using System.Collections.Generic;
using PickupObjects;

namespace Infastructure.Data
{
    [Serializable]
    public class AbilityData
    {
        public List<ProductType> PickedProducts = new List<ProductType>();
    }
}