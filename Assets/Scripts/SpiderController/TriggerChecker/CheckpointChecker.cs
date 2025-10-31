using CheckPointManagement;
using UnityEngine;

namespace SpiderController.TriggerChecker
{
    public class CheckpointChecker : ProductCheckerBase
    {
        protected override bool Accept(Collider col) =>
            TryGetProduct(col, typeof(CheckPoint));
    }
}