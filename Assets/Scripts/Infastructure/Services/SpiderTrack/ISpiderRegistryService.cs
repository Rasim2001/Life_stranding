using SpiderController;

namespace Infastructure.Services.SpiderTrack
{
    public interface ISpiderTrackService
    {
        Spider Spider { get; set; }
        string GetDistanceToGoal();
    }
}