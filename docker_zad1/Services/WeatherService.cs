using System.Text.Json.Nodes;

namespace docker_zad1.Services {
    public record WeatherDto(string City, double Temp, int Pressure, string Description);

    public interface IWeatherService {

        Task<WeatherDto> GetCurrentAsync(string city);
    }

    public class WeatherService : IWeatherService {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public WeatherService(HttpClient httpClient, IConfiguration configuration) {
            _httpClient = httpClient;
            _apiKey = configuration["OpenWeatherMap:ApiKey"] ?? "";
        }

        public async Task<WeatherDto> GetCurrentAsync(string city) {
            var url = $"https://api.openweathermap.org/data/2.5/weather?q={city}&appid={_apiKey}&units=metric";
            var json = await _httpClient.GetFromJsonAsync<JsonNode>(url) ?? throw new Exception("No API data");

            return new WeatherDto(
                City: json["name"]!.ToString(),
                Temp: json["main"]!["temp"]!.GetValue<double>(),
                Pressure: json["main"]!["pressure"]!.GetValue<int>(),
                Description: json["weather"]![0]!["description"]!.ToString()
            );
        }
    }
}