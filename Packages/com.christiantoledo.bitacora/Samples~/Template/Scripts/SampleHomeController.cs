using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bitacora.Samples
{
    public class SampleHomeController : MonoBehaviour
    {
        [Header("Countdown")]
        [SerializeField] private GameObject countdownContainer;
        [SerializeField] private TMP_Text countdownData;
        [SerializeField] private int countdownSeconds = 5;

        [Header("Participant")]
        [SerializeField] private GameObject participantContainer;
        [SerializeField] private TMP_InputField participantInput;

        [Header("Start button")]
        [SerializeField] private GameObject startButton;
        [SerializeField] private GameObject closeButton;
        
        [Header("Next Scene")]
        [SerializeField] private string nextScene = "SampleSimulation";

        private const string DefaultParticipant = "No informa";
        private bool isStarting;

        void Awake()
        {
            if (countdownContainer != null)
                countdownContainer.SetActive(false);

            #if UNITY_WEBGL && !UNITY_EDITOR
                // En el navegador no se puede cerrar la aplicación
                SetActive(closeButton, false);
            #endif
        }

        private void Start()
        {
            // El campo de participante solo se muestra si la configuración no es anónima
            AppManager app = AppManager.Instance;
            bool askParticipant = app != null && app.Config.ParticipantMode != ParticipantMode.Anonymous;
            SetActive(participantContainer, askParticipant);
        }

        public async void StartSession()
        {
            if (isStarting) return;

            if (string.IsNullOrEmpty(nextScene))
            {
                Debug.LogError($"[{nameof(SampleHomeController)}] No hay escena siguiente configurada.");
                return;
            }

            SessionManager sessions = SessionManager.Instance;
            
            if (sessions == null)
            {
                Debug.LogError($"[{nameof(SampleHomeController)}] No se encontró Bitácora en la escena.");
                return;
            }

            isStarting = true;
            SetActive(startButton, false);

            SessionInfo session = await sessions.StartSessionAsync(GetParticipant());

            if (session == null)
            {
                // El estado "Sin conexión" lo muestra ConnectionStatusView
                Debug.LogWarning($"[{nameof(SampleHomeController)}] No se pudo iniciar la sesión.");
                SetActive(startButton, true);
                isStarting = false;
                return;
            }
 
            Debug.Log($"[{nameof(SampleHomeController)}] Sesión iniciada como {session.Alias}.");

            SetActive(participantContainer, false);
            StartCoroutine(CountdownAndLoad());
        }

        /// <summary>Devuelve el dato ingresado, o null si el campo está vacío.</summary>
        private string GetParticipant()
        {
            if (participantInput == null || string.IsNullOrWhiteSpace(participantInput.text))
                return DefaultParticipant;

            return participantInput.text.Trim();
        }

        private IEnumerator CountdownAndLoad()
        {
            if (countdownContainer != null && countdownData != null)
            {
                countdownContainer.SetActive(true);

                for (int i = countdownSeconds; i > 0; i--)
                {
                    countdownData.text = $"{i}...";
                    yield return new WaitForSeconds(1f);
                }
            }
            else
            {
                yield return new WaitForSeconds(countdownSeconds);
            }

            SceneManager.LoadScene(nextScene);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null)
                target.SetActive(active);
        }

        public void CloseApp()
        {
            Debug.Log($"[{nameof(SampleHomeController)}] Salir de la aplicación.");

            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
    }
}
