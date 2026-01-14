using UnityEngine;

namespace Infastructure.CutScenes
{
    public class BorderCutsceneAnimator : MonoBehaviour
    {
        private BorderCutsceneUI[] _borders;

        private void Awake() =>
            _borders = GetComponentsInChildren<BorderCutsceneUI>();

        public void PlayAnimation()
        {
            foreach (BorderCutsceneUI border in _borders)
                border.Play();
        }
    }
}