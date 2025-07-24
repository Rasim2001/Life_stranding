using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _2
{
    public class PlaneIndicator : MonoBehaviour
    {
        [SerializeField] private Image _image;
        [SerializeField] private TextMeshProUGUI _text;

        public void Show() =>
            _image.color = Color.green;

        public void Hide() =>
            _image.color = Color.white;

        public void SelectMode(int index) =>
            _text.text = index.ToString();
    }
}