using System;
using Infastructure.Services.Defeat;
using UnityEngine;

namespace SpiderController.UI.Health
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

        private readonly IDefeatWindowService _defeatWindowService;
        private float _currentHp;


        public SpiderHealth(IDefeatWindowService defeatWindowService, float maxHp)
        {
            _defeatWindowService = defeatWindowService;

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
                _defeatWindowService.OpenDefeatWindow();
        }

        public void Heal(float amount) =>
            CurrentHP = Mathf.Min(CurrentHP + amount, MaxHp);
    }
}