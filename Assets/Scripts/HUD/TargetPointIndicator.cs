using UnityEngine;

namespace HUD
{
    public abstract class TargetPointIndicator
    {
        protected Vector3 FinishTargetPosition;

        private readonly ArrowUI _arrowUI;

        private readonly RectTransform _canvasRect;
        private readonly LayerMask _layerMask;

        private readonly float _borderOffsetX = 75;
        private readonly float _borderOffsetY = 75;

        private readonly Camera _mainCamera;
        private bool _arrowShowing;

        private readonly RectTransform _arrowRectTransform;
        private readonly RectTransform _arrowCenterRectTransform;

        protected TargetPointIndicator(ArrowUI arrowUI, RectTransform canvasRect, LayerMask layerMask)
        {
            _arrowUI = arrowUI;
            _canvasRect = canvasRect;
            _layerMask = layerMask;

            _arrowRectTransform = _arrowUI.GetComponent<RectTransform>();
            _arrowCenterRectTransform = _arrowUI.ArrowCenter.GetComponent<RectTransform>();

            _mainCamera = Camera.main;
        }

        public void HideTargetPoint() =>
            Show(false);

        public void ShowTargetPoint() =>
            Show(true);


        public virtual void Update()
        {
            Vector3 screenPos = _mainCamera.WorldToScreenPoint(FinishTargetPosition);

            bool isBehind = screenPos.z < 0;
            Vector2 canvasSize = _canvasRect.sizeDelta;

            if (isBehind)
            {
                screenPos.x = Screen.width - screenPos.x;
                screenPos.y = Screen.height - screenPos.y;
            }

            bool isOnScreen = screenPos.x > 0 && screenPos.x < Screen.width &&
                              screenPos.y > 0 && screenPos.y < Screen.height &&
                              !isBehind && IsTargetVisible();

            Show(!isOnScreen);

            Vector3 camLocal = _mainCamera.transform.InverseTransformPoint(FinishTargetPosition);
            camLocal.y = 0f;

            Vector2 direction = new Vector2(camLocal.x, camLocal.z).normalized;

            float halfWidth = canvasSize.x / 2f - _borderOffsetX;
            float halfHeight = canvasSize.y / 2f - _borderOffsetY;

            Vector2 clampedPos = GetClampedPosition(direction, halfWidth, halfHeight);
            _arrowRectTransform.anchoredPosition = clampedPos;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            _arrowCenterRectTransform.rotation = Quaternion.Euler(0, 0, angle);
        }

        private bool IsTargetVisible()
        {
            Vector3 cameraPos = _mainCamera.transform.position;
            Vector3 direction = FinishTargetPosition - cameraPos;
            float distance = direction.magnitude;

            return Physics.Raycast(cameraPos, direction.normalized, out RaycastHit hit, distance, _layerMask)
                ? hit.collider.GetComponent<TargetPointIndicatorMarker>()
                : false;
        }

        private Vector2 GetClampedPosition(Vector2 direction, float halfWidth, float halfHeight)
        {
            float t1 = halfWidth / Mathf.Abs(direction.x);
            float t2 = halfHeight / Mathf.Abs(direction.y);

            float t = Mathf.Min(t1, t2);

            return direction * t;
        }


        private void Show(bool value)
        {
            if (_arrowShowing == value)
                return;

            _arrowShowing = value;
            _arrowUI.gameObject.SetActive(value);
        }
    }
}