using Infastructure.Services.CameraProvider;
using PickupObjects.PickUpOnPlatform.FlowerManagement;
using UnityEngine;

namespace HUD
{
    public class FlowerPointIndicator : TargetPointIndicator
    {
        private readonly Flower _flower;

        public FlowerPointIndicator(ArrowUI arrowUI, RectTransform canvasRect, Flower
            flower, ICameraProviderService cameraProviderService) :
            base(arrowUI, canvasRect, cameraProviderService)
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