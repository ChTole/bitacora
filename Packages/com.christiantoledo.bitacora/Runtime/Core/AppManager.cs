using System;
using UnityEngine;

namespace Bitacora
{
    /// <summary>
    /// Punto de entrada de Bitácora. Carga la configuración, crea el backend,
    /// inicializa los demás componentes y mantiene el estado de conexión.
    /// Debe estar en un objeto raíz de la primera escena.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SessionManager), typeof(EventManager))]
    public sealed class AppManager : MonoBehaviour
    {
        public static AppManager Instance { get; private set; }

        [SerializeField] private BitacoraConfig config;

        public BitacoraConfig Config => config;
        public IBitacoraBackend Backend { get; private set; }
        public SessionManager Session { get; private set; }
        public EventManager Events { get; private set; }

        public ConnectionState Connection { get; private set; } = ConnectionState.Unknown;

        /// <summary>Se dispara cada vez que cambia el estado de conexión.</summary>
        public event Action<ConnectionState> ConnectionChanged;

        private bool isChecking;
        private float lastCheckTime;

        private void Awake()
        {
            // Si se vuelve a la escena inicial, ya existe una instancia: se descarta la nueva
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (config == null)
            {
                Debug.LogError($"[{nameof(AppManager)}] Falta asignar la configuración (BitacoraConfig).");
                enabled = false;
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Backend = CreateBackend(config);
            Session = GetComponent<SessionManager>();
            Events = GetComponent<EventManager>();

            Session.Initialize(this, Backend, Events, config);
            Events.Initialize(this, Backend, Session, config);
        }

        private async void Start()
        {
            if (Instance == this)
                await CheckConnectionAsync();
        }

        private void Update()
        {
            // Sin conexión, se reintenta cada cierto tiempo
            if (Connection == ConnectionState.Offline && !isChecking &&
                Time.unscaledTime - lastCheckTime >= config.ReconnectIntervalSeconds)
            {
                RecheckInBackground();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>Verifica la conexión con el servidor y actualiza el estado.</summary>
        public async Awaitable<bool> CheckConnectionAsync()
        {
            isChecking = true;
            lastCheckTime = Time.unscaledTime;
            SetConnection(ConnectionState.Connecting);

            bool online;
            try
            {
                online = await Backend.CheckConnectionAsync();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                online = false;
            }

            ReportConnectionResult(online);
            isChecking = false;
            return online;
        }

        /// <summary>Actualiza el estado según el resultado de cualquier solicitud al servidor.</summary>
        internal void ReportConnectionResult(bool success)
        {
            lastCheckTime = Time.unscaledTime;
            SetConnection(success ? ConnectionState.Online : ConnectionState.Offline);
        }

        private async void RecheckInBackground()
        {
            await CheckConnectionAsync();
        }

        private void SetConnection(ConnectionState state)
        {
            if (Connection == state) return;

            Connection = state;
            ConnectionChanged?.Invoke(state);
        }

        private static IBitacoraBackend CreateBackend(BitacoraConfig config)
        {
            switch (config.Backend)
            {
                case BackendType.Http:
                    return new HttpBackend(config);

                default:
                    return new MockBackend(config);
            }
        }
    }
}