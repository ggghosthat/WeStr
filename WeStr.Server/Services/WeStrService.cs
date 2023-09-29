using System.Text.Json;
using Grpc.Core;
using WeStr.Server.Contracts;

namespace WeStr.Server.Services;
public class WeStrService : Server.WeStrService.WeStrServiceBase
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory httpClientFactory;

    public WeStrService(IConfiguration configuration, 
                        IHttpClientFactory httpClientFactory)
    {
        this._configuration = configuration;
        this.httpClientFactory = httpClientFactory;
    }

    public override async Task<CurrentWeatherReply> GetCurrentWeather(CurrentWeatherRequest request,
                                                                ServerCallContext context)
    {
        var api = _configuration["API:Key"];
        var httpClient = httpClientFactory.CreateClient();
        var response = await httpClient.GetStreamAsync($"https://api.openweathermap.org/data/2.5/weather?lat={request.Lat}&lon={request.Lon}&appid={api}&units={request.Units}");

        var snap = JsonSerializer.Deserialize<WeStrSnap>(response);

        return new CurrentWeatherReply
        {
            Title = snap!.Weather[0].Main,
            Description = snap!.Weather[0].Description,
            Temp = snap!.Main.Temp,
            FeelsLike = snap!.Main.FeelsLike,
            Pressure = snap!.Main.Pressure,
            Humidity = snap!.Main.Humidity,
            SeaLevel = snap!.Main.SeaLevel,
            GrndLevel = snap!.Main.GrndLevel
        };
    }
}