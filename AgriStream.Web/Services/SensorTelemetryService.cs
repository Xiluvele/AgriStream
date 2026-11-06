
using System.Net.Http.Json;

namespace AgriStream.Web.Services
{
    public class SensorTelemetryService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public SensorTelemetryService(  HttpClient httpClient,  IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<List<SensorReadingDto>> GetReadingsAsync( int deviceId)
        {
            var baseUrl = _configuration["AzureFunctions:BaseUrl"]
                ?.TrimEnd('/');

            var functionKey = _configuration["AzureFunctions:TelemetryKey"];

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new InvalidOperationException(
                    "Azure Functions BaseUrl is not configured.");
            }

            var url = $"{baseUrl}/api/telemetry/{deviceId}";

            using var request = new HttpRequestMessage(  HttpMethod.Get, url);

            if (!string.IsNullOrWhiteSpace(functionKey))
            {
                request.Headers.Add("x-functions-key", functionKey);
            }

            using var response = await _httpClient.SendAsync(request);

            response.EnsureSuccessStatusCode();

            var readings = await response.Content
                .ReadFromJsonAsync<List<SensorReadingDto>>(
                    new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            return readings ?? new List<SensorReadingDto>();
        }
    }

    public class SensorReadingDto
    {
        public int DeviceId { get; set; }
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public double SoilMoisture { get; set; }
        public DateTimeOffset ReadingTime { get; set; }
    }
}
