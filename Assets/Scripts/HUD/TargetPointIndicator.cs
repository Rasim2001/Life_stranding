using UnityEngine;

namespace HUD
{
    public abstract class TargetPointIndicator
    {
        protected Vector3 FinishTargetPosition;

        private readonly RectTransform _arrowUI;
        private readonly RectTransform _canvasRect;
        private readonly LayerMask _layerMask;

        private readonly float _borderOffsetX = 50;
        private readonly float _borderOffsetY = 50f;

        private readonly Camera _mainCamera;
        private bool _arrowShowing;

        protected TargetPointIndicator(RectTransform arrowUI, RectTransform canvasRect, LayerMask layerMask)
        {
            _arrowUI = arrowUI;
            _canvasRect = canvasRect;
            _layerMask = layerMask;

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

            Vector2 screenPoint = new Vector2(screenPos.x, screenPos.y);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, screenPoint, null, out Vector2 localPoint);

            bool isOnScreen = screenPos.x > 0 && screenPos.x < Screen.width &&
                              screenPos.y > 0 && screenPos.y < Screen.height &&
                              !isBehind && IsTargetVisible();

            Show(!isOnScreen);

            Vector2 direction = (localPoint - Vector2.zero).normalized;

            float halfWidth = canvasSize.x / 2f - _borderOffsetX;
            float halfHeight = canvasSize.y / 2f - _borderOffsetY;

            Vector2 clampedPos = GetClampedPosition(direction, halfWidth, halfHeight);
            _arrowUI.anchoredPosition = clampedPos;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            _arrowUI.rotation = Quaternion.Euler(0, 0, angle);
        }

        private Vector2 GetClampedPosition(Vector2 direction, float halfWidth, float halfHeight)
        {
            float t1 = halfWidth / Mathf.Abs(direction.x);
            float t2 = halfHeight / Mathf.Abs(direction.y);

            float t = Mathf.Min(t1, t2);

            return direction * t;
        }

        private bool IsTargetVisible()
        {
            Vector3 cameraPos = _mainCamera.transform.position;
            Vector3 direction = FinishTargetPosition - cameraPos;
            float distance = direction.magnitude;


            if (Physics.Raycast(cameraPos, direction.normalized, out RaycastHit hit, distance, _layerMask))
            {
                Debug.DrawRay(cameraPos, direction.normalized * distance, Color.green, 0f);

                return hit.collider.GetComponent<TargetPointIndicatorMarker>();
            }

            Debug.DrawRay(cameraPos, direction.normalized * distance, Color.red, 0f);

            return false;
        }

        private void Show(bool value)
        {
            if (_arrowShowing == value)
                return;

            _arrowUI.gameObject.SetActive(value);
            _arrowShowing = value;
        }
    }
}