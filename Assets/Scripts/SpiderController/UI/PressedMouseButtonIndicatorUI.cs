using UnityEngine;
using UnityEngine.UI;

namespace SpiderController.UI
{
    public class PressedMouseButtonIndicatorUI : MonoBehaviour
    {
        [SerializeField] private Image _image;
        [SerializeField] private Color _pressedColor;

        public void Show() =>
            _image.color = Color.white;

        public void Hide() =>
            _image.color = _pressedColor;
    }
}