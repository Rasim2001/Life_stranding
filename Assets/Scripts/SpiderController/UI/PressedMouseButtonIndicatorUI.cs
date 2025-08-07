using UnityEngine;
using UnityEngine.UI;

namespace _2
{
    public class PressedMouseButtonIndicatorUI : MonoBehaviour
    {
        [SerializeField] private Image _image;
        [SerializeField] private Color _pressedColor;

        public void Show() =>
            _image.color = _pressedColor;

        public void Hide() =>
            _image.color = Color.white;
    }
}