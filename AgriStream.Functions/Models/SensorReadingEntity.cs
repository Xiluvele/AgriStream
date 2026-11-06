
using Azure;
using Azure.Data.Tables;
using System;

namespace AgriStream.Functions.Models
{
    public class SensorReadingEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = "";

        public string RowKey { get; set; } = "";

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }

        public int DeviceId { get; set; }

        public double Temperature { get; set; }

        public double Humidity { get; set; }

        public double SoilMoisture { get; set; }

        public DateTime ReadingTime { get; set; }
    }
}
