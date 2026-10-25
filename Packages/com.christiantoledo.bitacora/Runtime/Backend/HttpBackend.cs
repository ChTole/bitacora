using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Bitacora
{
    /// <summary>
    /// Backend que se comunica con un servidor real mediante HTTP, según el contrato de API.md.
    /// Usa UnityWebRequest, que funciona en escritorio, Android y navegador (WebGL).
    /// </summary>
    public sealed class HttpBackend : IBitacoraBackend
    {
        private const int MaxBatchSize = 100; // Límite de eventos por lote definido en API.md

        private readonly BitacoraConfig config;
        private readonly string baseUrl;

        public HttpBackend(BitacoraConfig config)
        {
            this.config = config;
            baseUrl = (config.ApiBaseUrl ?? string.Empty).TrimEnd('/') + "/v1";

            if (string.IsNullOrWhiteSpace(config.ApiBaseUrl))
                Debug.LogError($"[{nameof(HttpBackend)}] Falta configurar la URL del servidor (Api Base Url).");
        }

        public async Awaitable<bool> CheckConnectionAsync()
        {
            Response response = await SendAsync("GET", "/health");

            // El servidor responde pero rechaza la clave: es un error de configuración
            if (response.Status == 401)
                LogFailure("Clave rechazada por el servidor", response);

            return response.Status == 200;
        }

        public async Awaitable<SessionInfo> CreateSessionAsync(string participant)
        {
            string participantType = config.ParticipantMode.ToApiValue();

            object participantBody = participantType != null && !string.IsNullOrWhiteSpace(participant)
                ? new { type = participantType, value = participant }
                : null;

            var body = new
            {
                simulation_id = config.SimulationId,
                participant = participantBody
            };

            Response response = await SendAsync("POST", "/sessions", body);

            if (response.Status != 201)
            {
                LogFailure("No se pudo crear la sesión", response);
                return null;
            }

            try
            {
                return JsonConvert.DeserializeObject<SessionInfo>(response.Text);
            }
            catch (JsonException exception)
            {
                Debug.LogError($"[{nameof(HttpBackend)}] Respuesta de sesión con formato inválido: {exception.Message}");
                return null;
            }
        }

        public async Awaitable<bool> SendEventsAsync(string sessionId, IReadOnlyList<TelemetryEvent> events)
        {
            string path = $"/sessions/{Uri.EscapeDataString(sessionId)}/events";

            // Se respeta el límite del servidor dividiendo en lotes de hasta 100 eventos.
            // Si un lote falla, se reintentan todos: los ya recibidos se descartan como duplicados.
            for (int start = 0; start < events.Count; start += MaxBatchSize)
            {
                int count = Math.Min(MaxBatchSize, events.Count - start);
                var chunk = new List<TelemetryEvent>(count);
                for (int i = start; i < start + count; i++)
                    chunk.Add(events[i]);

                Response response = await SendAsync("POST", path, new { events = chunk });

                switch (response.Status)
                {
                    case 200:
                        continue;

                    case 409: // Sesión cerrada: los eventos ya no pueden registrarse
                    case 400: // Datos inválidos: reenviarlos no cambia el resultado
                    case 401: // Clave incorrecta: es un error de configuración
                    case 404: // La sesión no existe
                        LogFailure($"Se descartan {count} eventos", response);
                        continue;

                    default: // Sin conexión, 429 o error del servidor: conviene reintentar
                        LogFailure("No se pudo enviar el lote; se reintentará", response, warning: true);
                        return false;
                }
            }

            return true;
        }

        public async Awaitable<bool> FinishSessionAsync(string sessionId, SessionStatus status)
        {
            string path = $"/sessions/{Uri.EscapeDataString(sessionId)}/finish";
            Response response = await SendAsync("POST", path, new { status = status.ToApiValue() });

            if (response.Status == 200)
                return true;

            LogFailure("No se pudo cerrar la sesión", response);
            return response.Status == 404; // Si no existe, no hay nada que cerrar
        }

        // --- Comunicación -------------------------------------------------------------

        private readonly struct Response
        {
            public readonly long Status;   // 0 si no hubo respuesta (error de red o tiempo agotado)
            public readonly string Text;
            public readonly string NetworkError;

            public Response(long status, string text, string networkError)
            {
                Status = status;
                Text = text;
                NetworkError = networkError;
            }
        }

        private async Awaitable<Response> SendAsync(string method, string path, object body = null)
        {
            using (var request = new UnityWebRequest(baseUrl + path, method))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = config.RequestTimeoutSeconds;
                request.SetRequestHeader("X-Api-Key", config.ApiKey ?? string.Empty);

                if (body != null)
                {
                    byte[] json = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(body));
                    request.uploadHandler = new UploadHandlerRaw(json);
                    request.SetRequestHeader("Content-Type", "application/json");
                }

                UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                while (!operation.isDone)
                    await Awaitable.NextFrameAsync();

                bool noResponse = request.result == UnityWebRequest.Result.ConnectionError;

                return new Response(
                    noResponse ? 0 : request.responseCode,
                    request.downloadHandler?.text,
                    noResponse ? request.error : null);
            }
        }

        // --- Errores ----------------------------------------------------------------------

        private sealed class ErrorEnvelope
        {
            [JsonProperty("error")] public ErrorBody Error { get; set; }
        }

        private sealed class ErrorBody
        {
            [JsonProperty("code")] public string Code { get; set; }
            [JsonProperty("message")] public string Message { get; set; }
        }

        private static void LogFailure(string context, Response response, bool warning = false)
        {
            string detail;

            if (response.Status == 0)
            {
                detail = $"sin respuesta del servidor ({response.NetworkError})";
            }
            else
            {
                detail = $"HTTP {response.Status}";
                try
                {
                    ErrorBody error = JsonConvert.DeserializeObject<ErrorEnvelope>(response.Text ?? "")?.Error;
                    if (error != null)
                        detail += $" {error.Code}: {error.Message}";
                }
                catch (JsonException)
                {
                    // La respuesta no tiene el formato de error del contrato
                }
            }

            string message = $"[{nameof(HttpBackend)}] {context}: {detail}";
            if (warning) Debug.LogWarning(message);
            else Debug.LogError(message);
        }
    }
}