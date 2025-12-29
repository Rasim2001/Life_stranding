using System;
using System.Linq;
using UI;
using UI.Curtain;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using Zenject;

namespace Infastructure.Services.CutScene
{
    public class CutSceneService : ICutSceneService, ITickable
    {
        public event Action<bool> OnCutsceneActiveChanged;
        public event Action OnSkipHappened;
        public Action OnWeatherChanged { get; set; }
        public float LerpForwardSpeed { get; set; }
        public bool HasPlayed { get; set; }
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

        public CutSceneService(ICurtainRoot curtain) =>
            _curtain = curtain;


        public void Tick()
        {
            /*if (AnyKeyPressed() && !HasPlayed && _isActive)
                Skip();*/
        }

        public void Skip()
        {
            /*HasPlayed = true;
            _curtain.ShowAndHide();
            OnSkipHappened?.Invoke();*/
        }

        public void Clear()
        {
            HasPlayed = false;
            IsActive = false;
        }

        private bool AnyKeyPressed()
        {
            Gamepad gp = Gamepad.current;
            if (gp == null)
                return Input.anyKeyDown;

            return gp.allControls.Any(c => c is ButtonControl b && b.wasPressedThisFrame) ||
                   Input.anyKeyDown;
        }
    }
}