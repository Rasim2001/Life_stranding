using UnityEngine;

namespace HUD
{
    public class FinishPointIndicator : TargetPointIndicator
    {
        private readonly Transform _finishPoint;

        public FinishPointIndicator(ArrowUI arrowUI, RectTransform canvasRect, LayerMask layerMask,
            Transform finishPoint) : base(
            arrowUI, canvasRect, layerMask)
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