using System;
using DG.Tweening;
using Infastructure.Services.Window;
using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;
using Vector3 = UnityEngine.Vector3;

namespace UI
{
    public class ButtonScalerUI : MonoBehaviour, IPointerEnterHandler,
        ISelectHandler, IDeselectHandler
    {
        private readonly Vector3 _hoverSize = new Vector3(1.2f, 1.2f, 1.2f);
        private Tween _scaleTween;

        private IEventSystemSelector _eventSystemSelector;

        private bool _isSelected;

        [Inject]
        public void Construct(IEventSystemSelector eventSystemSelector) =>
            _eventSystemSelector = eventSystemSelector;

        private void Awake() =>
            _eventSystemSelector.OnSelectHappened += SelectHappened;

        private void OnDestroy()
        {
            _eventSystemSelector.OnSelectHappened -= SelectHappened;

            _scaleTween?.Kill();
        }

        private void SelectHappened(GameObject obj)
        {
            if (gameObject != obj)
                Deselect();
            else if (gameObject == obj && !_isSelected)
                Select();
        }


        public void OnPointerEnter(PointerEventData eventData) =>
            Select();


        public void OnSelect(BaseEventData eventData) =>
            Select();

        public void OnDeselect(BaseEventData eventData) =>
            Deselect();


        private void Select()
        {
            _eventSystemSelector.SelectButton(gameObject);
            _isSelected = true;


            _scaleTween?.Kill();
            _scaleTween = transform.DOScale(_hoverSize, 0.5f);
        }

        private void Deselect()
        {
            _isSelected = false;

            _scaleTween?.Kill();
            _scaleTween = transform.DOScale(Vector3.one, 0.25f);
        }
    }
}