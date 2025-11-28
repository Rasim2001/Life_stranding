using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.Buttons
{
    public class LanguageSwitcherArrowUI : MonoBehaviour, IPointerClickHandler
    {
        public Action OnClickHappened;

        private readonly Vector3 _hoverSize = new Vector3(1.5f, 1.5f, 1.5f);

        private Sequence _scaleSequence;

        private void Awake()
        {
            _scaleSequence = DOTween.Sequence()
                .Append(transform.DOScale(_hoverSize, 0.1f))
                .Append(transform.DOScale(Vector3.one, 0.1f))
                .SetAutoKill(false)
                .Pause();
        }

        public void OnPointerClick(PointerEventData eventData) =>
            OnClickHappened?.Invoke();

        public void Select()
        {
            if (_scaleSequence == null)
                return;

            _scaleSequence.Restart();
        }

        private void OnDestroy()
        {
            _scaleSequence?.Kill();
            _scaleSequence = null;
        }
    }
}