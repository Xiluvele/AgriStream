
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AgriStream.Functions
{
    public class ProcessIrrigationCommand
    {
        private readonly ILogger<ProcessIrrigationCommand> _logger;
        private readonly IConfiguration _configuration;

        public ProcessIrrigationCommand(
            ILogger<ProcessIrrigationCommand> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        // =====================================================
        // PROCESS IRRIGATION COMMAND FROM AZURE QUEUE STORAGE
        // =====================================================

        [Function("ProcessIrrigationCommand")]
        public async Task Run(
            [QueueTrigger(
                "irrigation-commands",
                Connection = "AzureWebJobsStorage")]
            string queueMessage)
        {
            _logger.LogInformation(
                "New irrigation command received.");

            // Convert queue JSON into a C# object.
            var command =
                JsonSerializer.Deserialize<IrrigationCommand>(
                    queueMessage,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            // Validate command.
            if (command == null ||
                command.DeviceId <= 0 ||
                string.IsNullOrWhiteSpace(command.CommandId) ||
                (command.Command != "Start" &&
                 command.Command != "Stop"))
            {
                throw new InvalidOperationException(
                    "Invalid irrigation command received.");
            }

            // =================================================
            // SIMULATE IRRIGATION DEVICE OPERATION
            // =================================================

            if (command.Command == "Start")
            {
                _logger.LogInformation(
                    "Starting irrigation for Device {DeviceId}",
                    command.DeviceId);
            }
            else
            {
                _logger.LogInformation(
                    "Stopping irrigation for Device {DeviceId}",
                    command.DeviceId);
            }

            // =================================================
            // CONNECT TO AZURE TABLE STORAGE
            // =================================================

            var connectionString =
                _configuration["AzureWebJobsStorage"];

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "AzureWebJobsStorage connection string is missing.");
            }

            var tableClient = new TableClient(
                connectionString,
                "IrrigationActivity");

            // Create table automatically if it does not exist.
            await tableClient.CreateIfNotExistsAsync();

            // =================================================
            // CREATE IRRIGATION ACTIVITY RECORD
            // =================================================

            var activity = new TableEntity(
                partitionKey: $"device-{command.DeviceId}",
                rowKey: command.CommandId)
            {
                ["DeviceId"] = command.DeviceId,
                ["Command"] = command.Command,
                ["Status"] = "Completed",
                ["RequestedBy"] = command.RequestedBy ?? "",
                ["RequestedAt"] = command.RequestedAt,
                ["ProcessedAt"] = DateTimeOffset.UtcNow
            };

            // =================================================
            // SAVE ACTIVITY TO AZURE TABLE STORAGE
            // =================================================

            // Upsert makes retries safe for the same CommandId.
            await tableClient.UpsertEntityAsync(
                activity,
                TableUpdateMode.Replace);

            _logger.LogInformation(
                "Irrigation activity saved to Azure Table Storage.");

            _logger.LogInformation(
                "Command ID: {CommandId}",
                command.CommandId);

            _logger.LogInformation(
                "Device ID: {DeviceId}",
                command.DeviceId);

            _logger.LogInformation(
                "Command: {Command}",
                command.Command);

            _logger.LogInformation(
                "Status: Completed");

            _logger.LogInformation(
                "Irrigation command processed successfully.");
        }
    }

    // =========================================================
    // IRRIGATION COMMAND MODEL
    // =========================================================

    public class IrrigationCommand
    {
        public string CommandId { get; set; } = "";

        public int DeviceId { get; set; }

        public string Command { get; set; } = "";

        public string RequestedBy { get; set; } = "";

        public DateTimeOffset RequestedAt { get; set; }
    }
}
