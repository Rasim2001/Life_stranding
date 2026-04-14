using PickupObjects;
using UnityEngine;

namespace SpiderController.TriggerChecker
{
    public class FlowerChecker : ProductCheckerBase
    {
        protected override bool Accept(Collider col) =>
            TryGetProduct(col, ProductType.Flower);
    }
}