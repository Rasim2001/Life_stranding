using Infastructure.Services.CameraProvider;
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

        protected TargetPointIndicator(ArrowUI arrowUI, RectTransform canvasRect, LayerMask layerMask,
            ICameraProviderService cameraProviderService)
        {
            _arrowUI = arrowUI;
            _canvasRect = canvasRect;
            _layerMask = layerMask;

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

            Transform camTr = _mainCamera.transform;

            Vector3 viewportPos = _mainCamera.WorldToViewportPoint(FinishTargetPosition);
            bool inFront = viewportPos.z > 0f;

            bool insideViewport =
                viewportPos.x > 0f && viewportPos.x < 1f &&
                viewportPos.y > 0f && viewportPos.y < 1f &&
                inFront;

            bool isOnScreen = insideViewport && IsTargetVisible();

            Show(!isOnScreen);
            if (isOnScreen)
                return;

            Vector3 vpForDir = viewportPos;
            if (vpForDir.z < 0f)
            {
                vpForDir.x = 1f - vpForDir.x;
                vpForDir.y = 1f - vpForDir.y;
            }

            Vector2 dirViewport = new Vector2(
                vpForDir.x - 0.5f,
                vpForDir.y - 0.5f
            );

            Vector3 toTarget = (FinishTargetPosition - camTr.position).normalized;
            float camX = Vector3.Dot(toTarget, camTr.right);
            float camY = Vector3.Dot(toTarget, camTr.forward);
            Vector2 dirCamera = new Vector2(camX, camY);

            if (dirViewport.sqrMagnitude < 0.0001f)
                dirViewport = Vector2.up;
            if (dirCamera.sqrMagnitude < 0.0001f)
                dirCamera = Vector2.up;

            dirViewport.Normalize();
            dirCamera.Normalize();

            float frontDot = Vector3.Dot(camTr.forward, toTarget); // -1..1

            float t = Mathf.InverseLerp(0.2f, -0.4f, frontDot);
            t = Mathf.Clamp01(t);

            Vector2 dir = Vector2.Lerp(dirViewport, dirCamera, t);
            dir.Normalize();

            Vector2 canvasSize = _canvasRect.sizeDelta;
            float halfWidth = canvasSize.x * 0.5f - _borderOffsetX;
            float halfHeight = canvasSize.y * 0.5f - _borderOffsetY;

            Vector2 clampedPos = GetClampedPosition(dir, halfWidth, halfHeight);
            _arrowRectTransform.anchoredPosition = clampedPos;

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            _arrowCenterRectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }


        private bool IsTargetVisible()
        {
            Vector3 cameraPos = _mainCamera.transform.position;
            Vector3 dir = FinishTargetPosition - cameraPos;
            float dist = dir.magnitude;

            if (!Physics.Raycast(cameraPos, dir.normalized, out RaycastHit hit, dist, _layerMask))
                return false;

            return hit.collider.GetComponent<TargetPointIndicatorMarker>() != null;
        }

        private Vector2 GetClampedPosition(Vector2 dir, float halfWidth, float halfHeight)
        {
            if (Mathf.Approximately(dir.x, 0f))
                dir.x = 0.0001f;
            if (Mathf.Approximately(dir.y, 0f))
                dir.y = 0.0001f;

            float tX = halfWidth / Mathf.Abs(dir.x);
            float tY = halfHeight / Mathf.Abs(dir.y);

            float t = Mathf.Min(tX, tY);

            return dir * t;
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