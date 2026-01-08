using PickupObjects.PickUpOnPlatform.FlowerManagement;
using SpiderController.UI.LastChanceQTE;
using UI.MVVM.Base;
using UnityEngine;

namespace Infastructure.Factories.GameFactories
{
    public interface IGameUIFactory
    {
        void CreateGamplayRoot();
        IWindowBinder CreateWindow(WindowViewModel viewModel, Transform container);
        void CreateLastChanceRoot(Flower flower, LastChanceBarUI lastChanceBarUI);
    }
}