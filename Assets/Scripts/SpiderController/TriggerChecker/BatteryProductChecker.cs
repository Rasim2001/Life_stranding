using PickupObjects;
using UnityEngine;

namespace SpiderController.TriggerChecker
{
    public class BatteryProductChecker : ProductCheckerBase
    {
        protected override bool Accept(Collider col) =>
            TryGetProduct(col, ProductType.Battery);
    }
}