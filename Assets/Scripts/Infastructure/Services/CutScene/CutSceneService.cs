using System;
using UnityEngine;

namespace Infastructure.Services.CutScene
{
    public class CutSceneService : ICutSceneService
    {
        public event Action<bool> OnCutsceneActiveChanged;

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

        private bool _isActive;
    }
}