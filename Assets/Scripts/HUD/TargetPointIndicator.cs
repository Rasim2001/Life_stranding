using Infastructure.Services.CameraProvider;
using UnityEngine;

namespace HUD
{
    public abstract class TargetPointIndicator
    {
        protected Vector3 FinishTargetPosition;

        private readonly ArrowUI _arrowUI;

        private readonly RectTransform _canvasRect;

        private readonly Camera _mainCamera;

        private readonly RectTransform _arrowRectTransform;
        private readonly RectTransform _arrowCenterRectTransform;

        private float OutOfSightOffest => outOfSightOffset * _canvasRect.localScale.x;
        private float outOfSightOffset = 50f;
        private bool _arrowShowingArrow;

        protected TargetPointIndicator(
            ArrowUI arrowUI,
            RectTransform canvasRect,
            ICameraProviderService cameraProviderService)
        {
            _arrowUI = arrowUI;
            _canvasRect = canvasRect;

            _arrowRectTransform = _arrowUI.GetComponent<RectTransform>();
            _arrowCenterRectTransform = _arrowUI.ArrowCenter;

            _mainCamera = cameraProviderService.Camera;
        }

        public void HideTargetPoint() =>
            Show(false);

        public void ShowTargetPoint() =>
            Show(true);

        public virtual void Update()
        {
            if (_mainCamera == null)
                return;

            SetIndicatorPosition();
        }

        private void SetIndicatorPosition()
        {
            Vector3 indicatorPosition = _mainCamera.WorldToScreenPoint(FinishTargetPosition);

            float width = _canvasRect.rect.width * _canvasRect.localScale.x;
            float height = _canvasRect.rect.height * _canvasRect.localScale.y;

            float padX = OutOfSightOffest + GetIconSafePaddingX();
            float padY = OutOfSightOffest + GetIconSafePaddingY();

            bool isInSafeArea =
                indicatorPosition.z >= 0f &&
                indicatorPosition.x >= padX &&
                indicatorPosition.x < width - padX &&
                indicatorPosition.y >= padY &&
                indicatorPosition.y < height - padY;

            ShowArrow(!isInSafeArea);

            if (isInSafeArea)
            {
                indicatorPosition.z = 0f;
            }
            else if (indicatorPosition.z >= 0f)
            {
                indicatorPosition = OutOfRangeIndicatorPosition(indicatorPosition);
            }
            else
            {
                indicatorPosition *= -1f;

                indicatorPosition = OutOfRangeIndicatorPosition(indicatorPosition);
            }

            _arrowCenterRectTransform.rotation = RotationOutOfSightTargetIndicator(indicatorPosition);

            _arrowRectTransform.position = indicatorPosition;
        }

        private float GetIconSafePaddingX()
        {
            float w = _arrowRectTransform.rect.width;
            float left = w * _arrowRectTransform.pivot.x;
            float right = w * (1f - _arrowRectTransform.pivot.x);
            return Mathf.Max(left, right);
        }

        private float GetIconSafePaddingY()
        {
            float h = _arrowRectTransform.rect.height;
            float bottom = h * _arrowRectTransform.pivot.y;
            float top = h * (1f - _arrowRectTransform.pivot.y);
            return Mathf.Max(bottom, top);
        }

        private Vector3 OutOfRangeIndicatorPosition(Vector3 indicatorPosition)
        {
            indicatorPosition.z = 0f;

            Vector3 canvasCenter = new Vector3(_canvasRect.rect.width / 2f, _canvasRect.rect.height / 2f, 0f) *
                                   _canvasRect.localScale.x;
            indicatorPosition -= canvasCenter;

            float divX = (_canvasRect.rect.width / 2f - OutOfSightOffest) / Mathf.Abs(indicatorPosition.x);
            float divY = (_canvasRect.rect.height / 2f - OutOfSightOffest) / Mathf.Abs(indicatorPosition.y);

            if (divX < divY)
            {
                float angle = Vector3.SignedAngle(Vector3.right, indicatorPosition, Vector3.forward);
                indicatorPosition.x = Mathf.Sign(indicatorPosition.x) *
                                      (_canvasRect.rect.width * 0.5f - OutOfSightOffest) * _canvasRect.localScale.x;
                indicatorPosition.y = Mathf.Tan(Mathf.Deg2Rad * angle) * indicatorPosition.x;
            }

            else
            {
                float angle = Vector3.SignedAngle(Vector3.up, indicatorPosition, Vector3.forward);

                indicatorPosition.y = Mathf.Sign(indicatorPosition.y) *
                                      (_canvasRect.rect.height / 2f - OutOfSightOffest) * _canvasRect.localScale.y;
                indicatorPosition.x = -Mathf.Tan(Mathf.Deg2Rad * angle) * indicatorPosition.y;
            }

            indicatorPosition += canvasCenter;

            return indicatorPosition;
        }


        private Quaternion RotationOutOfSightTargetIndicator(Vector3 indicatorPosition)
        {
            Vector3 canvasCenter = new Vector3(_canvasRect.rect.width / 2f, _canvasRect.rect.height / 2f, 0f) *
                                   _canvasRect.localScale.x;

            float angle = Vector3.SignedAngle(Vector3.up, indicatorPosition - canvasCenter, Vector3.forward);

            Vector3 targetAngle = new Vector3(0f, 0f, angle + 90);

            return Quaternion.Euler(targetAngle);
        }


        private void Show(bool value) =>
            _arrowUI.gameObject.SetActive(value);

        private void ShowArrow(bool value)
        {
            if (_arrowShowingArrow == value)
                return;

            _arrowShowingArrow = value;
            _arrowUI.ArrowCenter.gameObject.SetActive(value);
        }
    }
}