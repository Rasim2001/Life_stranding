using UnityEngine;
using UnityEngine.UI;

namespace UI.Buttons
{
    public class ButtonSelectedStartSplashUI : ButtonSelectedBaseUI
    {
        [SerializeField] private Image _selectImage;

        protected override void Awake()
        {
            base.Awake();

            ApplyDeselectedVisual();
        }

        protected override void OnSelected() =>
            _selectImage.enabled = true;

        protected override void OnDeselected() =>
            ApplyDeselectedVisual();

        private void ApplyDeselectedVisual() =>
            _selectImage.enabled = false;
    }
}