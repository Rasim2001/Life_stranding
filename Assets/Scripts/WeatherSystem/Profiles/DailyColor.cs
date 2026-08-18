using System;
using UnityEngine;

namespace WeatherSystem.Profiles
{
    // Цвет, который либо константа, либо зависит от времени суток. Шкала времени —
    // TimeOfDay01 из WeatherService: 0 — полночь, 0.25 — восход, 0.5 — полдень, 0.75 — закат
    // (см. WeatherTime.cs).
    [Serializable]
    public class DailyColor
    {
        public enum Mode { Constant, Gradient }

        [SerializeField] private Mode _mode = Mode.Constant;
        [SerializeField] private Color _constant = Color.white;
        [SerializeField] private Gradient _gradient = new Gradient();

        public Color Evaluate(float timeOfDay01) =>
            _mode == Mode.Constant ? _constant : _gradient.Evaluate(timeOfDay01);
    }
}
