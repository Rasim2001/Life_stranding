using Infastructure.Services.CameraProvider;
using UnityEngine;

namespace HUD
{
    public class FinishPointIndicator : TargetPointIndicator
    {
        private readonly Transform _finishPoint;

        public FinishPointIndicator(ArrowUI arrowUI, RectTransform canvasRect, LayerMask layerMask,
            Transform finishPoint, ICameraProviderService cameraProviderService) : base(
            arrowUI, canvasRect, layerMask, cameraProviderService)
        {
            _finishPoint = finishPoint;
        }

        public override void Update()
        {
            FinishTargetPosition = _finishPoint.position;

            base.Update();
        }
    }
}