using System;

namespace Infastructure.Services.Hint
{
    public interface IHintService
    {
        Action OnProductHint { get; set; }
        Action OnCheckpointHint { get; set; }
        Action OnGeneratorHint { get; set; }
    }
}