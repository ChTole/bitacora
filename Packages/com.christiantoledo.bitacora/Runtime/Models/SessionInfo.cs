using Newtonsoft.Json;

namespace Bitacora
{
    /// <summary>Datos de la sesión que devuelve el servidor al crearla.</summary>
    public sealed class SessionInfo
    {
        [JsonProperty("session_id")]
        public string SessionId { get; set; }

        [JsonProperty("alias")]
        public string Alias { get; set; }
    }
}