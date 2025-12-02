using System;

namespace Infastructure.Services.Hint
{
    public class HintReceiverService : IHintReceiverService
    {
        public Action OnProductHint { get; set; }
        public Action OnCheckpointHint { get; set; }
        public Action OnGeneratorHint { get; set; }
        public Action OnLastChanceHintShowHappened { get; set; }
        public Action OnLastChanceHintHideHappened { get; set; }
    }
}