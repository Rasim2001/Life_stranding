using Infastructure.Services.Window;
using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;

namespace UI.Buttons
{
    public abstract class ButtonSelectedBaseUI : MonoBehaviour,
        IPointerEnterHandler, ISelectHandler, IDeselectHandler
    {
        public bool IsSelected => _isSelected;

        protected IEventSystemSelector _eventSystemSelector;
        protected bool _isSelected;

        [Inject]
        public void Construct(IEventSystemSelector eventSystemSelector) =>
            _eventSystemSelector = eventSystemSelector;

        protected virtual void Awake()
        {
            if (_eventSystemSelector != null)
                _eventSystemSelector.OnSelectHappened += SelectHappened;
        }

        protected virtual void OnDestroy()
        {
            if (_eventSystemSelector != null)
                _eventSystemSelector.OnSelectHappened -= SelectHappened;

            OnCleanup();
        }

        private void SelectHappened(GameObject obj)
        {
            if (gameObject != obj)
                DeselectInternal();
            else if (!_isSelected)
                SelectInternal();
        }

        public void OnPointerEnter(PointerEventData eventData) =>
            SelectInternal();

        public void OnSelect(BaseEventData eventData) =>
            SelectInternal();

        public void OnDeselect(BaseEventData eventData) =>
            DeselectInternal();

        private void SelectInternal()
        {
            if (_isSelected)
                return;

            _isSelected = true;

            _eventSystemSelector?.SelectButton(gameObject);
            OnSelected();
        }

        private void DeselectInternal()
        {
            if (!_isSelected)
                return;

            _isSelected = false;
            OnDeselected();
        }

        protected abstract void OnSelected();
        protected abstract void OnDeselected();

        protected virtual void OnCleanup()
        {
        }
    }
}