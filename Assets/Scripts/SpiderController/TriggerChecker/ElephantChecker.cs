using PickupObjects;
using UnityEngine;

namespace SpiderController.TriggerChecker
{
    public class ElephantChecker : ProductCheckerBase
    {
        protected override bool Accept(Collider col) =>
            TryGetProduct(col, ProductType.Elephant);
    }
}