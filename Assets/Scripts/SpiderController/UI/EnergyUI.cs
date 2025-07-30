using UnityEngine;
using UnityEngine.UI;

namespace _2
{
    public class EnergyUI : MonoBehaviour
    {
        [SerializeField] private Image _energyImage;

        public float FillAmount => _energyImage.fillAmount;

        public void SetEnergyValue(float energyValue) =>
            _energyImage.fillAmount = energyValue;
    }
}