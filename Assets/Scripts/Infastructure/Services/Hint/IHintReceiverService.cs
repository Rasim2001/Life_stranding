using System;

namespace Infastructure.Services.Hint
{
    public interface IHintReceiverService
    {
        Action OnProductHint { get; set; }
        Action OnCheckpointHint { get; set; }
        Action OnGeneratorHint { get; set; }
        Action OnLastChanceHint { get; set; }
    }
}