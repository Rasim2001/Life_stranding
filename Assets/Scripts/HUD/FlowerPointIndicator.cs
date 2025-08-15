using UnityEngine;

namespace HUD
{
    public class FlowerPointIndicator : TargetPointIndicator
    {
        private readonly Transform _flowerTransform;

        public FlowerPointIndicator(RectTransform arrowUI, RectTransform canvasRect, LayerMask layerMask, Transform
            flowerTransform) :
            base(arrowUI, canvasRect, layerMask)
        {
            _flowerTransform = flowerTransform;
        }

        public override void Update()
        {
            FinishTargetPosition = _flowerTransform.position;

            base.Update();
        }

        public void HideTargetPoint() =>
            Show(false);

        public void ShowTargetPoint() =>
            Show(true);
    }
}