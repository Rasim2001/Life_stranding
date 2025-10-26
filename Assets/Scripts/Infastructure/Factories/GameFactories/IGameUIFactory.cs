using UI.MVVM.Base;
using UnityEngine;

namespace Infastructure.Factories.GameFactories
{
    public interface IGameUIFactory
    {
        void CreateGamplayRoot();
        IWindowBinder CreateWindow(WindowViewModel viewModel, Transform container);
    }
}