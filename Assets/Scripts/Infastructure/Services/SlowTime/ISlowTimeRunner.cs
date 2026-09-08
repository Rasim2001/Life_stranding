namespace Infastructure.Services.SlowTime
{
    public interface ISlowTimeRunner
    {
        void SlowDown();
        void StopSlowDown();
    }
}