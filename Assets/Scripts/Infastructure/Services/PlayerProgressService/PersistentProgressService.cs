using Infastructure.Data;

namespace Infastructure.Services.PlayerProgressService
{
    public class PersistentProgressService : IPersistentProgressService
    {
        public PlayerProgress PlayerProgress { get; set; }
    }
}