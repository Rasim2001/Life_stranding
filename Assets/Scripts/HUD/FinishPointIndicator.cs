using UnityEngine;

namespace HUD
{
    public class FinishPointIndicator : TargetPointIndicator
    {
        public FinishPointIndicator(RectTransform arrowUI, RectTransform canvasRect, LayerMask layerMask,
            Vector3 finishPosition) : base(
            arrowUI, canvasRect, layerMask)
        {
            FinishTargetPosition = finishPosition;
        }
    }
}