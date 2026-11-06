
using AgriStream.Functions.Models;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace AgriStream.Functions
{
    public class GetSensorReadings
    {
        private readonly TableServiceClient _tableServiceClient;

        public GetSensorReadings(IConfiguration configuration)
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

        [Function("GetSensorReadings")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "get",
                Route = "telemetry/{deviceId:int}")]
            HttpRequestData req,
            int deviceId)
        {
            if (deviceId <= 0)
            {
                var invalid = req.CreateResponse(
                    HttpStatusCode.BadRequest);

                await invalid.WriteStringAsync(
                    "Invalid device ID.");

                return invalid;
            }

            var tableClient = _tableServiceClient
                .GetTableClient("SensorReadings");

            var readings = new List<object>();

            await foreach (
                var reading in tableClient.QueryAsync<SensorReadingEntity>(
                    x => x.PartitionKey == $"device-{deviceId}"))
            {
                readings.Add(new
                {
                    reading.DeviceId,
                    reading.Temperature,
                    reading.Humidity,
                    reading.SoilMoisture,
                    reading.ReadingTime
                });
            }

            var response = req.CreateResponse(HttpStatusCode.OK);

            await response.WriteAsJsonAsync(readings);

            return response;
        }
    }
}

