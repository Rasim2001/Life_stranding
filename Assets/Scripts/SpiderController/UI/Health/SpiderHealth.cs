using System;
using UnityEngine;

namespace SpiderController
{
    public class SpiderHealth
    {
        public event Action HealthChanged;

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


        public void TakeDamage(float damage)
        {
            if (CurrentHP <= 0)
                return;

            CurrentHP -= damage;
        }
    }
}