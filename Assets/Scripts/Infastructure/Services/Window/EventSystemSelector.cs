using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Infastructure.Services.Window
{
    public class EventSystemSelector : MonoBehaviour, IEventSystemSelector
    {
        [SerializeField] private EventSystem _eventSystem;

        public event Action<GameObject> OnSelectHappened;

        private GameObject _lastSelectedButton;

        public void SelectButton(GameObject buttonObject)
        {
            if (_eventSystem.currentSelectedGameObject == buttonObject)
                return;

            _eventSystem.firstSelectedGameObject = buttonObject;
            _eventSystem.SetSelectedGameObject(buttonObject);

            _lastSelectedButton = buttonObject;

            OnSelectHappened?.Invoke(buttonObject);
        }

        public bool HasFocusUI() =>
            _eventSystem.currentSelectedGameObject != null;

        private void Update()
        {
            if (_eventSystem.currentSelectedGameObject == null && _lastSelectedButton != null)
                _eventSystem.SetSelectedGameObject(_lastSelectedButton.gameObject);
        }
    }
}