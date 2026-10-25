using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bitacora
{
    /// <summary>
    /// Registra los eventos de la sesión activa, los numera y los envía por lotes.
    /// Si un envío falla, los eventos quedan en la cola y se reintentan en el siguiente envío.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EventManager : MonoBehaviour
    {
        public static EventManager Instance =>
            AppManager.Instance != null ? AppManager.Instance.Events : null;

        /// <summary>Eventos registrados que todavía no se enviaron.</summary>
        public int PendingCount => queue.Count;

        private readonly List<TelemetryEvent> queue = new List<TelemetryEvent>();

        private AppManager app;
        private IBitacoraBackend backend;
        private SessionManager session;
        private BitacoraConfig config;

        private int nextSequence = 1;
        private bool isSending;
        private float lastFlushTime;

        internal void Initialize(AppManager app, IBitacoraBackend backend, SessionManager session, BitacoraConfig config)
        {
            this.app = app;
            this.backend = backend;
            this.session = session;
            this.config = config;
        }

        internal void ResetForNewSession()
        {
            queue.Clear();
            nextSequence = 1;
            lastFlushTime = Time.unscaledTime;
        }

        /// <summary>Registra un evento de la sesión activa.</summary>
        /// <param name="type">Tipo de evento, por ejemplo "hole_drilled".</param>
        /// <param name="data">Datos del evento, por ejemplo new { joint = 1 }.</param>
        public void Track(string type, object data = null)
        {
            if (session == null || !session.HasActiveSession || session.IsFinishing)
            {
                Debug.LogWarning($"[{nameof(EventManager)}] No hay una sesión activa: se descarta el evento \"{type}\".");
                return;
            }

            if (string.IsNullOrWhiteSpace(type))
            {
                Debug.LogWarning($"[{nameof(EventManager)}] Evento sin tipo: se descarta.");
                return;
            }

            queue.Add(new TelemetryEvent(nextSequence++, type, data));

            if (queue.Count >= config.BatchSize)
                FlushInBackground();
        }

        private void Update()
        {
            if (session == null || !session.HasActiveSession || queue.Count == 0 || isSending)
                return;

            if (Time.unscaledTime - lastFlushTime >= config.FlushIntervalSeconds)
                FlushInBackground();
        }

        /// <summary>Envía todos los eventos pendientes. Devuelve false si el envío falló.</summary>
        public async Awaitable<bool> FlushAsync()
        {
            // Si hay un envío en curso, se espera a que termine
            while (isSending)
                await Awaitable.NextFrameAsync();

            if (queue.Count == 0 || session == null || !session.HasActiveSession)
                return true;

            isSending = true;
            lastFlushTime = Time.unscaledTime;

            TelemetryEvent[] batch = queue.ToArray();

            bool sent;
            try
            {
                sent = await backend.SendEventsAsync(session.Current.SessionId, batch);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                sent = false;
            }

            app.ReportConnectionResult(sent);

            // Solo se quitan los eventos enviados: los registrados durante el envío quedan en la cola
            if (sent)
                queue.RemoveRange(0, batch.Length);

            isSending = false;
            return sent;
        }

        private async void FlushInBackground()
        {
            await FlushAsync();
        }
    }
}