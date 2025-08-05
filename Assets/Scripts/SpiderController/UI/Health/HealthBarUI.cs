using UnityEngine;
using UnityEngine.UI;

namespace SpiderController
{
    public class HealthBarUI : MonoBehaviour
    {
        [SerializeField] private Image _healthBar;

        public void SetValue(float currentHp, float maxHp) =>
            _healthBar.fillAmount = currentHp / maxHp;
    }
}