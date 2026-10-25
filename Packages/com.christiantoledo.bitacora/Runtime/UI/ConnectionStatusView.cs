using TMPro;
using UnityEngine;

namespace Bitacora
{
    /// <summary>
    /// Muestra el estado de conexión en un texto. Se agrega al canvas de cualquier escena
    /// y se actualiza solo al cambiar el estado.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ConnectionStatusView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;

        [Header("Textos")]
        [SerializeField] private string unknownText = "Sin verificar";
        [SerializeField] private string connectingText = "Conectando...";
        [SerializeField] private string onlineText = "Conectado";
        [SerializeField] private string offlineText = "Sin conexión";

        [Header("Colores")]
        [SerializeField] private bool useColors = true;
        [SerializeField] private Color unknownColor = new Color(0.6f, 0.6f, 0.6f);
        [SerializeField] private Color connectingColor = new Color(0.95f, 0.75f, 0.2f);
        [SerializeField] private Color onlineColor = new Color(0.3f, 0.8f, 0.4f);
        [SerializeField] private Color offlineColor = new Color(0.9f, 0.3f, 0.3f);

        private AppManager subscribedTo;

        private void Reset()
        {
            // Al agregar el componente, toma el texto del mismo objeto si existe
            label = GetComponent<TMP_Text>();
        }

        private void OnEnable() => Subscribe();
        private void Start() => Subscribe(); // por si Bitácora todavía no existía en OnEnable
        private void OnDisable() => Unsubscribe();

        private void Subscribe()
        {
            AppManager app = AppManager.Instance;

            if (app == null)
            {
                Show(ConnectionState.Unknown);
                return;
            }

            if (subscribedTo == app) return;

            Unsubscribe();
            subscribedTo = app;
            app.ConnectionChanged += Show;
            Show(app.Connection);
        }

        private void Unsubscribe()
        {
            if (subscribedTo != null)
                subscribedTo.ConnectionChanged -= Show;

            subscribedTo = null;
        }

        private void Show(ConnectionState state)
        {
            if (label == null) return;

            switch (state)
            {
                case ConnectionState.Connecting:
                    Apply(connectingText, connectingColor);
                    break;
                case ConnectionState.Online:
                    Apply(onlineText, onlineColor);
                    break;
                case ConnectionState.Offline:
                    Apply(offlineText, offlineColor);
                    break;
                default:
                    Apply(unknownText, unknownColor);
                    break;
            }
        }

        private void Apply(string text, Color color)
        {
            label.text = text;

            if (useColors)
                label.color = color;
        }
    }
}