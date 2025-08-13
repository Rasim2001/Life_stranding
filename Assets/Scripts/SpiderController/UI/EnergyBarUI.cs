using UnityEngine;
using UnityEngine.UI;

namespace SpiderController.UI
{
    public class EnergyBarUI : MonoBehaviour
    {
        [SerializeField] private Image _energyImage;

        public void SetEnergyValue(float energyValue) =>
            _energyImage.fillAmount = energyValue;
    }
}