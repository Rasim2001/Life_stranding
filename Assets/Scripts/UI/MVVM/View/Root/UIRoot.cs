using UnityEngine;

namespace UI.MVVM.View.Root
{
    public class UIRoot : MonoBehaviour, IUIRoot
    {
        public void AttachSceneUI(GameObject sceneUI) =>
            sceneUI.transform.SetParent(transform, false);
    }
}