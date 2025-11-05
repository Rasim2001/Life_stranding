using Common;
using UnityEngine;

namespace SpiderController.TriggerChecker
{
    public class GeneratorChecker : ProductCheckerBase
    {
        protected override bool Accept(Collider col) =>
            TryGetProduct(col, typeof(Generator));
    }
}