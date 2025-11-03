using PickupObjects;
using PickupObjects.PickUpOnPlatform;
using PickupObjects.PickUpOnPlatform.FlowerManagement;
using UnityEngine;

namespace HUD
{
    public class FlowerPointIndicator : TargetPointIndicator
    {
        private readonly Flower _flower;

        public FlowerPointIndicator(ArrowUI arrowUI, RectTransform canvasRect, LayerMask layerMask, Flower
            flower) :
            base(arrowUI, canvasRect, layerMask)
        {
            _flower = flower;
        }

        public override void Update()
        {
            if (_flower.IsOnPlatform)
                return;

            FinishTargetPosition = _flower.transform.position;

            base.Update();
        }
    }
}