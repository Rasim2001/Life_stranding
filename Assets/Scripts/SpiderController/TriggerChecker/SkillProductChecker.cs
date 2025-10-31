using PickupObjects.Skills;
using UnityEngine;

namespace SpiderController.TriggerChecker
{
    public class SkillProductChecker : ProductCheckerBase
    {
        protected override bool Accept(Collider col) =>
            TryGetProduct(col, typeof(SkillProduct));
    }
}