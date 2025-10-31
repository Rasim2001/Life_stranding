using Infastructure.Services.Window;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace UI.MVVM.Base
{
    public abstract class PopupBinder<T> : WindowBinder<T> where T : WindowViewModel
    {
        [SerializeField] private Button _btnClose;
        [SerializeField] private Button _btnCloseAlt;

        private IEventSystemSelector _eventSystemSelector;

        [Inject]
        public void Construct(IEventSystemSelector eventSystemSelector) =>
            _eventSystemSelector = eventSystemSelector;

        protected virtual void Awake() =>
            _eventSystemSelector.SelectButton(_btnClose.gameObject);

        protected virtual void Start()
        {
            _btnClose?.onClick.AddListener(OnCloseButtonClick);
            _btnCloseAlt?.onClick.AddListener(OnCloseButtonClick);
        }

        protected virtual void OnDestroy()
        {
            _btnClose?.onClick.RemoveListener(OnCloseButtonClick);
            _btnCloseAlt?.onClick.RemoveListener(OnCloseButtonClick);
        }

        protected virtual void OnCloseButtonClick() =>
            ViewModel.RequestClose();
    }
}