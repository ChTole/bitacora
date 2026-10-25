using System;
using UnityEngine;

namespace Bitacora
{
    /// <summary>
    /// Crea, conserva y cierra la sesión activa. También cierra la sesión como abandonada
    /// si la aplicación se cierra, y registra las pausas (por ejemplo, al quitarse el visor).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SessionManager : MonoBehaviour
    {
        public const string PausedEvent = "session_paused";
        public const string ResumedEvent = "session_resumed";
        public static SessionManager Instance =>
            AppManager.Instance != null ? AppManager.Instance.Session : null;

        /// <summary>Sesión activa, o null si no hay ninguna.</summary>
        public SessionInfo Current { get; private set; }
        public bool HasActiveSession => Current != null;
        public bool IsFinishing { get; private set; }
        
        public event Action<SessionInfo> SessionStarted;
        public event Action<SessionStatus> SessionFinished;

        private AppManager app;
        private IBitacoraBackend backend;
        private EventManager events;
        private BitacoraConfig config;

        private bool quitRequested;
        private bool allowQuit;

        internal void Initialize(
            AppManager app, 
            IBitacoraBackend backend, 
            EventManager events, 
            BitacoraConfig config
        )
        {
            this.app = app;
            this.backend = backend;
            this.events = events;
            this.config = config;
        }

        private void OnEnable() => Application.wantsToQuit += OnWantsToQuit;
        private void OnDisable() => Application.wantsToQuit -= OnWantsToQuit;

        /// <summary>
        /// Crea una sesión en el servidor. Si el modo es anónimo, el participante se ignora.
        /// Devuelve null si no se pudo crear.
        /// </summary>
        public async Awaitable<SessionInfo> StartSessionAsync(string participant = null)
        {
            if (HasActiveSession)
            {
                Debug.LogWarning($"[{nameof(SessionManager)}] Ya hay una sesión activa.");
                return Current;
            }
 
            string identifier = config.ParticipantMode == ParticipantMode.Anonymous ? null : participant;
 
            SessionInfo session;
            try
            {
                session = await backend.CreateSessionAsync(identifier);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                session = null;
            }
 
            bool created = session != null && !string.IsNullOrEmpty(session.SessionId);
            app.ReportConnectionResult(created);
 
            if (!created)
            {
                Debug.LogWarning($"[{nameof(SessionManager)}] No se pudo crear la sesión.");
                return null;
            }
 
            Current = session;
            events.ResetForNewSession();
            SessionStarted?.Invoke(session);
            return session;
        }

        /// <summary>Envía los eventos pendientes y cierra la sesión activa.</summary>
        public async Awaitable FinishSessionAsync(SessionStatus status = SessionStatus.Completed)
        {
            if (!HasActiveSession || IsFinishing) return;
 
            IsFinishing = true;
 
            await events.FlushAsync();
 
            bool closed;
            try
            {
                closed = await backend.FinishSessionAsync(Current.SessionId, status);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                closed = false;
            }
 
            app.ReportConnectionResult(closed);
 
            Current = null;
            IsFinishing = false;
            SessionFinished?.Invoke(status);
        }

        // Android y Meta Quest: la app se pausa al ir al menú o al quitarse el visor.
        // La sesión continúa al volver; se registra la interrupción y se envía lo pendiente.
        private void OnApplicationPause(bool paused)
        {
            if (!HasActiveSession || IsFinishing) return;
 
            events.Track(paused ? PausedEvent : ResumedEvent);
 
            if (paused)
                FlushInBackground();
        }
 
        // Al cerrar la aplicación con una sesión activa, se demora el cierre
        // hasta cerrar la sesión como abandonada, con un tiempo máximo.
        private bool OnWantsToQuit()
        {
            if (allowQuit || !HasActiveSession)
                return true;
 
            FinishThenQuit();
            return false;
        }
 
        private async void FinishThenQuit()
        {
            if (quitRequested) return;
            quitRequested = true;
 
            FinishInBackground(SessionStatus.Abandoned);
 
            float deadline = Time.realtimeSinceStartup + config.QuitTimeoutSeconds;
            while (HasActiveSession && Time.realtimeSinceStartup < deadline)
                await Awaitable.NextFrameAsync();
 
            allowQuit = true;
            Application.Quit();
        }
 
        private async void FinishInBackground(SessionStatus status)
        {
            await FinishSessionAsync(status);
        }
 
        private async void FlushInBackground()
        {
            await events.FlushAsync();
        }
    }
}
