using System.Collections.Generic;
using UnityEngine;

namespace Bitacora
{
    /// <summary>
    /// Contrato con el servidor. El resto del package solo conoce esta interfaz,
    /// así puede usarse un servidor simulado o uno real sin otros cambios.
    /// </summary>
    public interface IBitacoraBackend
    {
        /// <summary>Verifica que el servidor responda.</summary>
        Awaitable<bool> CheckConnectionAsync();

        /// <summary>Crea una sesión. <paramref name="participant"/> es null si es anónima.</summary>
        Awaitable<SessionInfo> CreateSessionAsync(string participant);

        /// <summary>
        /// Envía un lote de eventos. Devuelve false si conviene reintentar (sin conexión o error
        /// del servidor) y true si los eventos ya no deben reenviarse (recibidos o descartados).
        /// </summary>
        Awaitable<bool> SendEventsAsync(string sessionId, IReadOnlyList<TelemetryEvent> events);

        /// <summary>Cierra la sesión.</summary>
        Awaitable<bool> FinishSessionAsync(string sessionId, SessionStatus status);
    }
}