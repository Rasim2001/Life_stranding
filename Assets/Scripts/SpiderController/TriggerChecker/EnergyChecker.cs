using PickupObjects;
using UnityEngine;

namespace SpiderController.TriggerChecker
{
    public class EnergyChecker : ProductCheckerBase
    {
        protected override bool Accept(Collider col) =>
            TryGetProduct(col, ProductType.Energy);
    }
}