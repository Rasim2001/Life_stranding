using System;

namespace Infastructure.Services.Hint
{
    public interface IHintReceiverService
    {
        Action OnProductHint { get; set; }
        Action OnCheckpointHint { get; set; }
        Action OnGeneratorHint { get; set; }
        Action OnLastChanceHintHideHappened { get; set; }
        Action OnLastChanceHintShowHappened { get; set; }
    }
}