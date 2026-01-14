using Common;
using Common.Biosphere;
using UnityEngine;

namespace SpiderController.TriggerChecker
{
    public class BiosphereChecker : ProductCheckerBase
    {
        protected override bool Accept(Collider col) =>
            TryGetProduct(col, typeof(BiosphereCheckpointIndicator));
    }
}