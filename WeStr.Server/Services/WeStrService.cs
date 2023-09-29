using System.Text.Json;
using System.Net;
using Grpc.Core;
using WeStr.Server.Contracts;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;

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

    public async Task<WeStrSnap> GetCurrentSnap(CurrentWeatherRequest request,
                                                HttpClient httpClient)
    {
        var api = _configuration["API:Key"];   
        var response = await httpClient.GetStreamAsync($"https://api.openweathermap.org/data/2.5/weather?lat={request.Lat}&lon={request.Lon}&appid={api}&units={request.Units}");

        return JsonSerializer.Deserialize<WeStrSnap>(response);
    }

    public override async Task<CurrentWeatherReply> GetCurrentWeather(CurrentWeatherRequest request,
                                                                      ServerCallContext context)
    {
        var httpClient = httpClientFactory.CreateClient();

        var snap = await GetCurrentSnap(request, httpClient);
        return new CurrentWeatherReply
        {
            Title = snap!.Weather[0].Main,
            Description = snap!.Weather[0].Description,
            Temp = snap!.Main.Temp,
            FeelsLike = snap!.Main.FeelsLike,
            Pressure = snap!.Main.Pressure,
            Humidity = snap!.Main.Humidity,
            SeaLevel = snap!.Main.SeaLevel,
            GrndLevel = snap!.Main.GrndLevel,
            Timestamp = Timestamp.FromDateTime(DateTime.UtcNow)
        };
    }

    public override async Task GetCurrentWeatherStream(CurrentWeatherRequest request,
                                                       IServerStreamWriter<CurrentWeatherReply> responseStream,
                                                       ServerCallContext context)
    {
        var httpClient = httpClientFactory.CreateClient();

        while(true)
        {
            if(context.CancellationToken.IsCancellationRequested)
            {
                break;
            }

            var snap = await GetCurrentSnap(request, httpClient);   

            await responseStream.WriteAsync(new CurrentWeatherReply
            {
                Title = snap!.Weather[0].Main,
                Description = snap!.Weather[0].Description,
                Temp = snap!.Main.Temp,
                FeelsLike = snap!.Main.FeelsLike,
                Pressure = snap!.Main.Pressure,
                Humidity = snap!.Main.Humidity,
                SeaLevel = snap!.Main.SeaLevel,
                GrndLevel = snap!.Main.GrndLevel,
                Timestamp = Timestamp.FromDateTime(DateTime.UtcNow)
            });         

            await Task.Delay(3000);
        }
    }
}