using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Bitacora
{
    /// <summary>
    /// Servidor simulado: responde con datos fijos o aleatorios y muestra todo en la consola.
    /// Permite desarrollar y probar el flujo completo sin un servidor real.
    /// La demora y la desconexión se leen de la configuración en cada solicitud,
    /// así pueden cambiarse durante el modo Play.
    /// </summary>
    public sealed class MockBackend : IBitacoraBackend
    {
        private static readonly string[] Animals =
            { "Carpincho", "Hornero", "Yaguareté", "Ñandú", "Guanaco", "Tatú", "Pingüino", "Puma" };

        // Adjetivos y colores que no cambian con el género del animal
        private static readonly string[] Qualities =
            { "Turquesa", "Violeta", "Celeste", "Naranja", "Veloz", "Valiente", "Audaz", "Amable" };

        private readonly BitacoraConfig config;

        public MockBackend(BitacoraConfig config)
        {
            this.config = config;
        }

        private bool Offline => config.MockSimulateOffline;

        public async Awaitable<bool> CheckConnectionAsync()
        {
            await SimulateLatency();
            Log(Offline ? "Sin conexión (simulado)." : "Conexión verificada.");
            return !Offline;
        }

        public async Awaitable<SessionInfo> CreateSessionAsync(string participant)
        {
            await SimulateLatency();

            if (Offline)
            {
                Log("No se pudo crear la sesión: sin conexión (simulado).");
                return null;
            }

            var session = new SessionInfo
            {
                SessionId = Guid.NewGuid().ToString(),
                Alias = RandomAlias()
            };

            Log($"Sesión creada: {session.SessionId} ({session.Alias}). " +
                $"Participante: {participant ?? "anónimo"}");
            return session;
        }

        public async Awaitable<bool> SendEventsAsync(string sessionId, IReadOnlyList<TelemetryEvent> events)
        {
            await SimulateLatency();

            if (Offline)
            {
                Log($"No se pudo enviar el lote de {events.Count} eventos: sin conexión (simulado).");
                return false;
            }

            string json = JsonConvert.SerializeObject(events, Formatting.Indented);
            Log($"Lote de {events.Count} eventos para la sesión {sessionId}:\n{json}");
            return true;
        }

        public async Awaitable<bool> FinishSessionAsync(string sessionId, SessionStatus status)
        {
            await SimulateLatency();

            if (Offline)
            {
                Log($"No se pudo cerrar la sesión {sessionId}: sin conexión (simulado).");
                return false;
            }

            Log($"Sesión {sessionId} cerrada: {status}.");
            return true;
        }

        private Awaitable SimulateLatency() =>
            Awaitable.WaitForSecondsAsync(Mathf.Max(0f, config.MockLatencySeconds));

        private static string RandomAlias() =>
            $"{Animals[UnityEngine.Random.Range(0, Animals.Length)]} " +
            $"{Qualities[UnityEngine.Random.Range(0, Qualities.Length)]}";

        private static void Log(string message) => Debug.Log($"[{nameof(MockBackend)}] {message}");
    }
}