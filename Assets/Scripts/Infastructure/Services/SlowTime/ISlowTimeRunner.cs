using Cysharp.Threading.Tasks;

namespace Infastructure.Services.SlowTime
{
    public interface ISlowTimeRunner
    {
        void SlowDown();
        void Resume();
        void Pause();
        void StopSlowDown();
    }
}