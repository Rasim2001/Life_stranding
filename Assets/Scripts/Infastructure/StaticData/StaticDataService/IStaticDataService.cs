using Infastructure.StaticData.Spider;

namespace Infastructure.StaticData.StaticDataService
{
    public interface IStaticDataService
    {
        void LoadStaticData();
        SpiderStaticData SpiderStaticData { get; }
    }
}