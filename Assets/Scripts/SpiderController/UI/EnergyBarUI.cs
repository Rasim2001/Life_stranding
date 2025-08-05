using UnityEngine;
using UnityEngine.UI;

namespace _2
{
    public class EnergyBarUI : MonoBehaviour
    {
        [SerializeField] private Image _energyImage;

        public void SetEnergyValue(float energyValue) =>
            _energyImage.fillAmount = energyValue;
    }
}