using System;
using UnityEngine;

namespace SpiderController.UI.Health
{
    public class SpiderHealth
    {
        public event Action HealthChanged;
        public event Action OnDefeatHappened;

        public float MaxHp { get; }
        public float CurrentHP
        {
            get => _currentHp;
            private set
            {
                if (!Mathf.Approximately(_currentHp, value))
                {
                    _currentHp = value;
                    HealthChanged?.Invoke();
                }
            }
        }

        private float _currentHp;


        public SpiderHealth(float maxHp)
        {
            CurrentHP = maxHp;
            MaxHp = maxHp;
        }

        public void Reset() =>
            CurrentHP = MaxHp;


        public void TakeDamage(float damage)
        {
            if (CurrentHP > 0)
                CurrentHP -= damage;

            if (CurrentHP <= 0)
                OnDefeatHappened?.Invoke();
        }
    }
}