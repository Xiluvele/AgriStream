
using AgriStream.Functions.Models;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Text.Json;

namespace AgriStream.Functions
{
    public class AddSensorReading
    {
        private readonly TableServiceClient _tableServiceClient;

        public AddSensorReading(IConfiguration configuration)
        {
            var connectionString =
                configuration["AzureWebJobsStorage"];

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "AzureWebJobsStorage is not configured.");
            }

            _tableServiceClient =
                new TableServiceClient(connectionString);
        }

        public class SensorReadingRequest
        {
            public int DeviceId { get; set; }

            public double Temperature { get; set; }

            public double Humidity { get; set; }

            public double SoilMoisture { get; set; }
        }

        [Function("AddSensorReading")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "post",
                Route = "telemetry")]
            HttpRequestData req)
        {
            SensorReadingRequest? reading;

            try
            {
                reading =
                    await JsonSerializer.DeserializeAsync<SensorReadingRequest>(
                        req.Body,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
            }
            catch (JsonException)
            {
                var invalidJson = req.CreateResponse(
                    HttpStatusCode.BadRequest);

                await invalidJson.WriteStringAsync(
                    "Invalid JSON request.");

                return invalidJson;
            }

            if (reading == null ||
                reading.DeviceId <= 0 ||
                !double.IsFinite(reading.Temperature) ||
                !double.IsFinite(reading.Humidity) ||
                !double.IsFinite(reading.SoilMoisture) ||
                reading.Temperature < -50 ||
                reading.Temperature > 100 ||
                reading.Humidity < 0 ||
                reading.Humidity > 100 ||
                reading.SoilMoisture < 0 ||
                reading.SoilMoisture > 100)
            {
                var invalid = req.CreateResponse(
                    HttpStatusCode.BadRequest);

                await invalid.WriteStringAsync(
                    "Invalid sensor reading values.");

                return invalid;
            }

            var tableClient = _tableServiceClient
                .GetTableClient("SensorReadings");

            await tableClient.CreateIfNotExistsAsync();

            var entity = new SensorReadingEntity
            {
                PartitionKey = $"device-{reading.DeviceId}",
                RowKey = Guid.NewGuid().ToString("N"),
                DeviceId = reading.DeviceId,
                Temperature = reading.Temperature,
                Humidity = reading.Humidity,
                SoilMoisture = reading.SoilMoisture,
                ReadingTime = DateTime.UtcNow
            };

            await tableClient.AddEntityAsync(entity);

            var response = req.CreateResponse(
                HttpStatusCode.Created);

            await response.WriteAsJsonAsync(new
            {
                Message = "Sensor reading saved successfully.",
                entity.DeviceId,
                entity.Temperature,
                entity.Humidity,
                entity.SoilMoisture,
                entity.ReadingTime
            });

            return response;
        }
    }
}
