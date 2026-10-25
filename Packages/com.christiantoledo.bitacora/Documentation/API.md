# Bitácora: contrato con el servidor

Este documento define cómo se comunica Bitácora con un servidor. Cualquier servidor que lo respete puede recibir la telemetría de una simulación: el servidor de prueba incluido en el repositorio, o un backend propio.

**Versión del contrato:** 1

## Convenciones

- Todas las rutas comienzan con `/v1`, a continuación de la URL configurada en `apiBaseUrl`.
- Las solicitudes y respuestas usan JSON (`Content-Type: application/json`).
- Los nombres de los campos usan `snake_case`.
- Las fechas usan ISO 8601 en UTC, por ejemplo `2026-10-23T14:05:12.345Z`.

## Autenticación

Todas las solicitudes incluyen el encabezado:

```
X-Api-Key: <clave>
```

La clave identifica a la simulación o al dispositivo. En compilaciones para navegador la clave es visible para cualquiera, por lo que el servidor debe limitar lo que permite hacer con ella: solo crear sesiones y registrar eventos de la simulación a la que pertenece.

## Flujo

```
GET  /v1/health                         Verificar la conexión
POST /v1/sessions                       Crear la sesión → session_id y alias
POST /v1/sessions/{session_id}/events   Enviar eventos (una o más veces)
POST /v1/sessions/{session_id}/finish   Cerrar la sesión
```

## Endpoints

### Verificar la conexión

```
GET /v1/health
```

**Respuesta `200`**

```json
{ "status": "ok" }
```

### Crear una sesión

```
POST /v1/sessions
```

**Solicitud**

```json
{
  "simulation_id": "taller-ensamblado",
  "participant": null
}
```

`participant` es `null` en las sesiones anónimas. Si la simulación identifica a la persona, contiene el tipo de dato y su valor:

```json
{
  "simulation_id": "taller-ensamblado",
  "participant": { "type": "email", "value": "persona@ejemplo.com" }
}
```

| `type` | Modo de participante en Bitácora |
|---|---|
| `email` | `Email` |
| `full_name` | `FullName` |
| `document_id` | `DocumentId` |

**Respuesta `201`**

```json
{
  "session_id": "27a02397-c169-4165-ba40-7402227e9d7e",
  "alias": "Carpincho Turquesa",
  "started_at": "2026-10-23T14:05:00.000Z"
}
```

- `session_id` lo genera el servidor. El cliente nunca lo genera.
- `alias` lo asigna el servidor a todas las sesiones, anónimas o no (ver [Alias](#alias)).

### Enviar eventos

```
POST /v1/sessions/{session_id}/events
```

**Solicitud**

```json
{
  "events": [
    {
      "sequence": 1,
      "timestamp": "2026-10-23T14:05:12.345Z",
      "type": "hole_drilled",
      "data": { "joint": 1 }
    },
    {
      "sequence": 2,
      "timestamp": "2026-10-23T14:05:15.020Z",
      "type": "screw_driven",
      "data": { "joint": 1, "drilled": true }
    }
  ]
}
```

| Campo | Reglas |
|---|---|
| `sequence` | Entero desde 1, incremental dentro de la sesión. Identifica al evento. |
| `timestamp` | Momento real del evento, en el dispositivo. |
| `type` | Texto en `snake_case`, hasta 64 caracteres. |
| `data` | Objeto JSON o `null`, hasta 4 KB. Su contenido depende de cada simulación. |

Un lote admite hasta 100 eventos.

**Respuesta `200`**

```json
{ "accepted": 2, "duplicates": 0 }
```

**Duplicados.** Si llega un evento con un `sequence` ya registrado en la sesión, el servidor lo ignora y lo cuenta en `duplicates`, sin devolver error. Esto permite que el cliente reenvíe un lote completo si no recibió la respuesta, sin generar datos repetidos.

### Cerrar una sesión

```
POST /v1/sessions/{session_id}/finish
```

**Solicitud**

```json
{ "status": "completed" }
```

| `status` | Significado |
|---|---|
| `completed` | La persona terminó la actividad. |
| `abandoned` | La aplicación se cerró o se volvió al inicio antes de terminar. |

**Respuesta `200`**

```json
{
  "session_id": "27a02397-c169-4165-ba40-7402227e9d7e",
  "status": "completed",
  "finished_at": "2026-10-23T14:12:40.000Z"
}
```

Cerrar una sesión ya cerrada devuelve `200` con los datos del primer cierre, para que el cliente pueda reintentar sin error.

## Eventos del package

Además de los eventos de cada simulación, Bitácora registra por su cuenta:

| `type` | Cuándo |
|---|---|
| `session_paused` | La aplicación se pausa (por ejemplo, al quitarse el visor) |
| `session_resumed` | La aplicación vuelve de una pausa |

Las simulaciones no deben usar tipos que comiencen con `session_`.

## Alias

El servidor asigna a cada sesión un alias legible, formado por un animal y un adjetivo o color, por ejemplo "Carpincho Turquesa". Permite que la persona encuentre después sus resultados sin haber dado ningún dato personal, incluso desde dispositivos donde no se puede redirigir a una página web, como un visor de realidad virtual.

- Los adjetivos y colores no cambian con el género del animal (turquesa, veloz, valiente), para que cualquier combinación sea correcta.
- El servidor evita repetir un alias entre las sesiones recientes. Si las combinaciones se agotan, puede agregar un número: "Carpincho Turquesa 27".

## Errores

Las respuestas de error tienen el formato:

```json
{
  "error": {
    "code": "session_finished",
    "message": "La sesión ya fue cerrada."
  }
}
```

| Código HTTP | `code` | Causa | El cliente... |
|---|---|---|---|
| `400` | `invalid_request` | Datos con formato incorrecto | No reintenta |
| `401` | `invalid_api_key` | Clave ausente o incorrecta | No reintenta |
| `404` | `session_not_found` | La sesión no existe | No reintenta |
| `409` | `session_finished` | Eventos enviados a una sesión cerrada | Descarta los eventos |
| `413` | `batch_too_large` | Más de 100 eventos en un lote | Divide el lote |
| `429` | `rate_limited` | Demasiadas solicitudes | Reintenta más tarde |
| `5xx` | `server_error` | Error del servidor | Reintenta más tarde |

Los errores de red (sin respuesta) se tratan como `5xx`: el cliente conserva los eventos y reintenta.

## Navegador (WebGL)

El servidor debe habilitar CORS para el dominio donde se publica la simulación, incluyendo el encabezado `X-Api-Key`.

## Pendiente

- **Cierre al cerrar la pestaña del navegador:** requiere un endpoint compatible con `navigator.sendBeacon`, que no permite encabezados personalizados.
- **Vinculación de dispositivos:** endpoints para obtener las credenciales de un visor mediante un código.