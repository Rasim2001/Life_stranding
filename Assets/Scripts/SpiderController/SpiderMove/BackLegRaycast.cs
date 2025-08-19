using UnityEngine;

namespace SpiderController.SpiderMove
{
    public class BackLegRaycast : MonoBehaviour
    {
        [SerializeField] private Vector3 _forwardRotation;
        [SerializeField] private Vector3 _backRotation;

        private bool _isBackState;

        public void SetBackStateLeg()
        {
            if (_isBackState)
                return;

            _isBackState = true;

            transform.localEulerAngles = _backRotation;
        }

        public void SetForwardStateLeg()
        {
            if (!_isBackState)
                return;

            _isBackState = false;

            transform.localEulerAngles = _forwardRotation;
        }
    }
}