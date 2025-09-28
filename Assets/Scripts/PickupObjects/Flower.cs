using System;
using HUD;

namespace PickupObjects
{
    public class Flower : PickupObjectBase
    {
        public Action OnDroppedFromPlatform;

        private FlowerPointIndicator _flowerPointIndicator;


        public void Initialize(FlowerPointIndicator flowerPointIndicator) =>
            _flowerPointIndicator = flowerPointIndicator;

        public override void StopSimulatePhysics()
        {
            base.StopSimulatePhysics();

            _flowerPointIndicator.HideTargetPoint();
        }

        protected override void StartSimulatePhysics()
        {
            base.StartSimulatePhysics();

            _flowerPointIndicator.ShowTargetPoint();

            OnDroppedFromPlatform?.Invoke();
        }
    }
}