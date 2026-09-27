using System;
using UnityEngine;

namespace WeatherSystem.Profiles
{
    // Float, который либо константа, либо зависит от времени суток — тот же смысл, что
    // у DailyColor, но для скалярных настроек (Gradient Exponent, Cloud Coverage и т.п.).
    [Serializable]
    public class DailyFloat
    {
        public enum Mode { Constant, Curve }

        [SerializeField] private Mode _mode = Mode.Constant;
        [SerializeField] private float _constant;
        [SerializeField] private AnimationCurve _curve = AnimationCurve.Constant(0f, 1f, 0f);

        public float Evaluate(float timeOfDay01) =>
            _mode == Mode.Constant ? _constant : _curve.Evaluate(timeOfDay01);
    }
}
