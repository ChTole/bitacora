using System;
using System.Globalization;
using Newtonsoft.Json;

namespace Bitacora
{
    /// <summary>Un evento registrado durante la sesión.</summary>
    public sealed class TelemetryEvent
    {
        [JsonProperty("sequence")]
        public int Sequence { get; }

        [JsonProperty("timestamp")]
        public string Timestamp { get; }

        [JsonProperty("type")]
        public string Type { get; }

        [JsonProperty("data")]
        public object Data { get; }

        public TelemetryEvent(int sequence, string type, object data)
        {
            Sequence = sequence;
            Type = type;
            Data = data;
            Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture); // ISO 8601 en UTC
        }
    }
}