using Infastructure.StaticData.HUD;
using Infastructure.StaticData.Spider;

namespace Infastructure.StaticData.StaticDataService
{
    public interface IStaticDataService
    {
        void LoadStaticData();
        SpiderStaticData SpiderStaticData { get; }
        HudStaticData HudStaticData { get; }
        GameStaticData GameStaticData { get; }
    }
}