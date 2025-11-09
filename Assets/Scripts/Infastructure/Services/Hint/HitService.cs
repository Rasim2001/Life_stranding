using System;

namespace Infastructure.Services.Hint
{
    public class HintService : IHintService
    {
        public Action OnProductHint { get; set; }
        public Action OnCheckpointHint { get; set; }
        public Action OnGeneratorHint { get; set; }
    }
}