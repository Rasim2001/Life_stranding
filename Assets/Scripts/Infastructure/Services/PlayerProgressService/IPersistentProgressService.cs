using Infastructure.Data;

namespace Infastructure.Services.PlayerProgressService
{
    public interface IPersistentProgressService
    {
        PlayerProgress PlayerProgress { get; set; }
    }
}