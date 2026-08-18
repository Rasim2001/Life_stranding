namespace WeatherSystem
{
    // Публичные поля намеренно шире, чем нужно тикету 02: будущий климат-модуль (вне scope v1)
    // подключается к ним, а не встраивается в эту систему заново.
    public interface IWeatherService
    {
        float NormalizedAltitude { get; }
        float TimeOfDay01 { get; }
    }
}
