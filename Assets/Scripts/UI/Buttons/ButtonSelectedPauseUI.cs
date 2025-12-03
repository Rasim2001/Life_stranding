using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Buttons
{
    public class ButtonSelectedPauseUI : ButtonSelectedBaseUI
    {
        [SerializeField] private Image _selectImage;
        [SerializeField] private TextMeshProUGUI _text;

        private readonly float _deselectedAlpha = 45f / 255f;

        protected override void Awake()
        {
            base.Awake();

            ApplyDeselectedVisual();
        }

        protected override void OnSelected()
        {
            if (_text != null)
            {
                Color c = _text.color;
                c.a = 1f;
                _text.color = c;
            }

            _selectImage.enabled = true;
        }

        protected override void OnDeselected() =>
            ApplyDeselectedVisual();

        private void ApplyDeselectedVisual()
        {
            Color c = _text.color;
            c.a = _deselectedAlpha;
            _text.color = c;

            _selectImage.enabled = false;
        }
    }
}