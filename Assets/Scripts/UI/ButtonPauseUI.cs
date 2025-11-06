using Infastructure.Services.Window;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zenject;

namespace UI
{
    public class ButtonPauseUI : MonoBehaviour, IPointerEnterHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField] private Image _selectImage;
        [SerializeField] private TextMeshProUGUI _text;

        private IEventSystemSelector _eventSystemSelector;

        private bool _isSelected;

        [Inject]
        public void Construct(IEventSystemSelector eventSystemSelector) =>
            _eventSystemSelector = eventSystemSelector;

        private void Awake() =>
            _eventSystemSelector.OnSelectHappened += SelectHappened;

        private void OnDestroy() =>
            _eventSystemSelector.OnSelectHappened -= SelectHappened;

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

            Color newColor = _text.color;
            newColor.a = 1;
            _text.color = newColor;

            _selectImage.enabled = true;
        }

        private void Deselect()
        {
            _isSelected = false;

            Color newColor = _text.color;
            newColor.a = 45 / 255f;
            _text.color = newColor;

            _selectImage.enabled = false;
        }
    }
}