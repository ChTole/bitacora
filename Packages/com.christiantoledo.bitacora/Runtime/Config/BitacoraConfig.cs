using UnityEngine;

namespace Bitacora
{
    /// <summary>Configuración de Bitácora. Se crea un asset por perfil de compilación.</summary>
    [CreateAssetMenu(fileName = "BitacoraConfig", menuName = "Bitácora/Configuración")]
    public sealed class BitacoraConfig : ScriptableObject
    {
        [Header("Servidor")]
        [SerializeField] private BackendType backend = BackendType.Mock;
        [SerializeField] private string apiBaseUrl = "http://127.0.0.1:8000";
        [Tooltip("Visible para cualquiera en compilaciones para navegador: no usar claves secretas.")]
        [SerializeField] private string apiKey = "dev-key";
        [SerializeField] private string simulationId = "sample";
        [Tooltip("Tiempo máximo de espera de cada solicitud, en segundos.")]
        [SerializeField, Min(1)] private int requestTimeoutSeconds = 10;

        [Header("Participante")]
        [SerializeField] private ParticipantMode participantMode = ParticipantMode.Anonymous;

        [Header("Envío de eventos")]
        [Tooltip("Cantidad de eventos que dispara un envío.")]
        [SerializeField, Min(1)] private int batchSize = 10;
        [Tooltip("Segundos entre envíos, aunque no se complete el lote.")]
        [SerializeField, Min(0.5f)] private float flushIntervalSeconds = 5f;

        [Header("Conexión y cierre")]
        [Tooltip("Segundos entre reintentos de conexión cuando no hay servidor.")]
        [SerializeField, Min(1f)] private float reconnectIntervalSeconds = 10f;
        [Tooltip("Tiempo máximo para cerrar la sesión al salir de la aplicación.")]
        [SerializeField, Min(0.5f)] private float quitTimeoutSeconds = 2f;

        [Header("Servidor simulado")]
        [Tooltip("Demora simulada de cada respuesta, en segundos.")]
        [SerializeField, Min(0f)] private float mockLatencySeconds = 0.3f;
        [Tooltip("Hace fallar todas las solicitudes. Se puede cambiar durante el modo Play.")]
        [SerializeField] private bool mockSimulateOffline;

        public BackendType Backend => backend;
        public string ApiBaseUrl => apiBaseUrl;
        public string ApiKey => apiKey;
        public string SimulationId => simulationId;
        public int RequestTimeoutSeconds => requestTimeoutSeconds;
        public ParticipantMode ParticipantMode => participantMode;
        public int BatchSize => batchSize;
        public float FlushIntervalSeconds => flushIntervalSeconds;
        public float ReconnectIntervalSeconds => reconnectIntervalSeconds;
        public float QuitTimeoutSeconds => quitTimeoutSeconds;
        public float MockLatencySeconds => mockLatencySeconds;
        public bool MockSimulateOffline => mockSimulateOffline;
    }
}