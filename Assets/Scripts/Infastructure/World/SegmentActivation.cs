namespace Infastructure.World
{
    public interface ISegmentActivation
    {
        void ActivateAll();
    }

    /// <summary>
    /// Точка вызова активации живых веток зафиксирована здесь тикетом 02
    /// (BuildLevelState.InitGameWorld, последней строкой). Механизм и наполнение
    /// ActivateAll() — тикет 04 (.scratch/scene-regulations-rollout/issues/04-live-root-marker-and-validator.md).
    /// Тело намеренно пустое: это не забытый код.
    /// </summary>
    public class SegmentActivation : ISegmentActivation
    {
        public void ActivateAll()
        {
        }
    }
}
