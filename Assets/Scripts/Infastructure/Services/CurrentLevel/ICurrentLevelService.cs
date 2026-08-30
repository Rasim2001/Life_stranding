namespace Infastructure.Services.CurrentLevel
{
    /// <summary>
    /// Объект «текущий уровень» вместо имени активной сцены (scene-architecture.md §1.5).
    /// </summary>
    public interface ICurrentLevelService
    {
        string LevelDataKey { get; }
        bool ShowsFirstEncounter { get; }
    }
}
