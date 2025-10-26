using System;
using UnityEngine;

namespace Infastructure.Services.Window
{
    public interface IEventSystemSelector
    {
        void SelectButton(GameObject buttonObject);
        event Action<GameObject> OnSelectHappened;
        bool HasFocusUI();
    }
}