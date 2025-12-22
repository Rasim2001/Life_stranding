using System;
using SpiderController.UI.LastChanceQTE;

namespace Infastructure.Services.QTE
{
    public interface ILastChanceQTEService
    {
        void StartQTE();
        void Initialize(LastChanceRootUI lastChanceRootUI, LastChanceBarUI lastChanceBarUI);
        event Action OnSaveHappened;
    }
}