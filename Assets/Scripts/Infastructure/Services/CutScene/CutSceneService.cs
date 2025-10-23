using System;
using UI;
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

        private readonly ICurtainRoot _cutSceneService;
        private bool _isActive;

        public CutSceneService(ICurtainRoot cutSceneService) =>
            _cutSceneService = cutSceneService;

        public void Tick()
        {
            if (Input.GetKeyDown(KeyCode.Space) && IsActive)
            {
                _cutSceneService.Show();

                OnSkipHappened?.Invoke();
            }
        }
    }
}