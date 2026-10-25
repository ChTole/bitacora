using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bitacora.Samples
{
    public class SampleSimulationController : MonoBehaviour
    {
        [Header("Session (opcional)")]
        [Tooltip("Texto donde se muestra el alias de la sesión.")]
        [SerializeField] private TMP_Text aliasData;

        [Header("Home Scene")]
        [SerializeField] private string homeScene = "SampleHome";

        private bool isFinishing;

        private void Start()
        {
            SessionManager sessions = SessionManager.Instance;
 
            if (sessions == null || !sessions.HasActiveSession)
            {
                Debug.LogWarning($"[{nameof(SampleSimulationController)}] No hay una sesión activa. " +
                                 "Iniciá el modo Play desde SampleHome.");
                return;
            }
 
            if (aliasData != null)
                aliasData.text = sessions.Current.Alias;
        }
 
        // Asignado al OnClick de cada botón, con el tipo de evento como parámetro:
        // "sample_event_a", "sample_event_b", "sample_event_c", "sample_event_d"

        public void TrackEvent(string eventType)
        {
            if (isFinishing) return;
 
            EventManager events = EventManager.Instance;
            if (events == null)
            {
                Debug.LogWarning($"[{nameof(SampleSimulationController)}] No se encontró Bitácora.");
                return;
            }
 
            double elapsed = Math.Round(Time.timeSinceLevelLoad, 2);
            events.Track(eventType, new { elapsed_seconds = elapsed });
 
            Debug.Log($"[{nameof(SampleSimulationController)}] Evento: {eventType} " +
                      $"(pendientes: {events.PendingCount})");
        }

        // Asignado al OnClick del botón Finalizar sesión
        public async void FinishSession()
        {
            if (isFinishing) return;
            isFinishing = true;
 
            Debug.Log($"[{nameof(SampleSimulationController)}] Sesión finalizada.");
 
            SessionManager sessions = SessionManager.Instance;
            if (sessions != null)
                await sessions.FinishSessionAsync(SessionStatus.Completed);
 
            if (!string.IsNullOrEmpty(homeScene))
                SceneManager.LoadScene(homeScene);
        }
    }
}
