using UnityEngine;

namespace HUD
{
    public class FinishIndicator
    {
        private readonly Vector3 _finishTargetPosition;
        private readonly RectTransform _arrowUI;
        private readonly RectTransform _canvasRect;
        private readonly LayerMask _layerMask;

        private readonly int _spiderLayer = LayerMask.NameToLayer("Spider");
        private readonly int _spiderColliderLayer = LayerMask.NameToLayer("SpiderCollider");
        private readonly int _flowerLayer = LayerMask.NameToLayer("Flower");

        private readonly float _borderOffsetX = 50;
        private readonly float _borderOffsetY = 100f;

        private readonly Camera _mainCamera;
        private bool _arrowShowing;

        public FinishIndicator(Vector3 finishTargetPosition, RectTransform arrowUI, RectTransform canvasRect,
            LayerMask layerMask)
        {
            _finishTargetPosition = finishTargetPosition;
            _arrowUI = arrowUI;
            _canvasRect = canvasRect;

            _layerMask = layerMask;
            _layerMask &= ~(1 << _spiderLayer) | (1 << _spiderColliderLayer) | (1 << _flowerLayer);

            _mainCamera = Camera.main;
        }


        public void Update()
        {
            Vector3 screenPos = _mainCamera.WorldToScreenPoint(_finishTargetPosition);

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
                              !isBehind && IsTargetVisible(_finishTargetPosition);

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

        private bool IsTargetVisible(Vector3 targetWorldPosition)
        {
            Vector3 cameraPos = _mainCamera.transform.position;
            Vector3 direction = targetWorldPosition - cameraPos;
            float distance = direction.magnitude;


            if (Physics.Raycast(cameraPos, direction.normalized, out RaycastHit hit, distance, _layerMask))
                return hit.collider.GetComponent<FinishTargetMarker>();

            return true;
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