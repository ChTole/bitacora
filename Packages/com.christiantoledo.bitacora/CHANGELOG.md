# Historial de cambios

Todos los cambios relevantes de Bitácora se registran en este archivo.

El formato sigue [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/) y el package usa [versionado semántico](https://semver.org/lang/es/).

## [0.2.0] - 2026-11-23

### Agregado

- Contrato 1.1: límite de 2000 eventos por sesión, error `409 session_full`, campo `discarded` en la respuesta de eventos y sección "Límites" en `API.md`.
- `HttpBackend` avisa en la consola cuando el servidor descarta eventos porque la sesión alcanzó el límite.
- Servidor de prueba: implementa el contrato 1.1. La variable `BITACORA_MAX_SESSION_EVENTS` permite bajar el límite para probar `session_full`.

### Cambiado

- `API.md` indica que, ante cualquier `409`, el cliente descarta los eventos sin importar el `code`. `HttpBackend` ya lo hacía, así que la versión 0.1.0 sigue siendo compatible con servidores del contrato 1.1.

## [0.1.0] - AAAA-MM-DD

### Agregado

- Sesiones que persisten entre escenas, con alias asignado por el servidor.
- Registro de eventos con número de secuencia, para ordenarlos y evitar duplicados.
- Envío por lotes con reintentos ante fallos de conexión.
- Cierre automático de la sesión al pausar o cerrar la aplicación.
- Backend simulado y `HttpBackend`, compatible con escritorio, navegador (WebGL) y Android.
- Indicador de estado de conexión (`ConnectionStatusView`).
- Escenas plantilla `Home` y `Simulation`, disponibles en Samples.
- Contrato con el servidor (`API.md`, versión 1) y servidor de prueba en Flask.

[0.2.0]: https://github.com/ChTole/bitacora/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/ChTole/bitacora/releases/tag/v0.1.0