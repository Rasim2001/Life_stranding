using System;
using UI;
using UI.Curtain;
using UnityEngine;
using Zenject;

namespace Infastructure.Services.CutScene
{
    public class CutSceneService : ICutSceneService, ITickable
    {
        public event Action<bool> OnCutsceneActiveChanged;
        public event Action OnSkipHappened;

        public float LerpForwardSpeed { get; set; }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                    OnCutsceneActiveChanged?.Invoke(value);

                _isActive = value;
            }
        }

        private readonly ICurtainRoot _curtain;

        private bool _isActive;
        private bool _isTriggered;

        public CutSceneService(ICurtainRoot curtain) =>
            _curtain = curtain;

        public void Tick()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && !_isTriggered)
            {
                _isTriggered = true;
                IsActive = false;

                _curtain.ShowAndHide();
                OnSkipHappened?.Invoke();
            }
        }
    }
}