# Bitácora

Package de Unity para registrar sesiones y eventos de telemetría en simulaciones educativas y experiencias interactivas, y enviarlos a un servidor mediante una API REST.

Está pensado como punto de partida: incluye dos escenas de ejemplo y los componentes necesarios para iniciar una sesión, registrar eventos y entregarlos al servidor, de forma independiente de la lógica de cada simulación.

## Características

- Participación anónima, con un alias asignado por el servidor (por ejemplo, "Carpincho Turquesa"), o identificada con un único dato configurable.
- Sesiones que persisten entre escenas.
- Registro de eventos con número de secuencia, para ordenarlos y evitar duplicados.
- Envío por lotes con reintentos ante fallos de conexión.
- Registro de pausas (por ejemplo, al quitarse el visor) y cierre de la sesión como abandonada si la aplicación se cierra.
- Indicador de estado de conexión listo para usar.
- Backend simulado para desarrollar y probar sin servidor, y servidor de prueba local incluido.
- Compatible con escritorio, navegador (WebGL) y Android.

## Requisitos

- Unity 6.3 o superior.
- Las dependencias (`com.unity.ugui` y `com.unity.nuget.newtonsoft-json`) se instalan automáticamente.

## Instalación

En Unity: **Window → Package Manager → + → Install package from git URL** y pegar:

```
https://github.com/ChTole/bitacora.git?path=/Packages/com.christiantoledo.bitacora#v0.1.0
```

El final `#v0.1.0` fija la versión. Sin él se instala la última versión del repositorio.

## Primeros pasos

1. Si el proyecto todavía no usa TextMeshPro, importar sus recursos:
   **Window → TextMeshPro → Import TMP Essential Resources**.
2. En el Package Manager, seleccionar **Bitácora**, pestaña **Samples**, y presionar **Import** en **Template**.
3. Agregar las escenas `SampleHome` y `SampleSimulation` a **File → Build Profiles → Scene List**, con `SampleHome` primero.
4. Abrir `SampleHome` y presionar Play.

La plantilla usa el backend simulado: funciona sin servidor y muestra en la consola todo lo que se enviaría.

## Flujo

1. **Configuración:** se carga al iniciar la aplicación.
2. **Conexión:** se verifica el servidor y se informa el estado; sin conexión, se reintenta periódicamente.
3. **Participante:** según el modo configurado, anónimo o identificado.
4. **Sesión:** el servidor la crea y devuelve su identificador y alias.
5. **Eventos:** la simulación los registra; el package los numera, agrupa y entrega.
6. **Cierre:** al finalizar la actividad, o como sesión abandonada si la aplicación se cierra.

## Responsabilidades

**Bitácora se encarga de:** la configuración, la conexión, la sesión, la entrega de eventos y el cierre.

**Cada simulación se encarga de:** su lógica, qué eventos registra y su interfaz. Las escenas de ejemplo son solo eso: un ejemplo.

## Uso

```csharp
// Iniciar la sesión (por ejemplo, desde la pantalla de inicio)
SessionInfo session = await SessionManager.Instance.StartSessionAsync();

// Registrar eventos durante la simulación
EventManager.Instance.Track("hole_drilled", new { joint = 1 });

// Cerrar la sesión al terminar
await SessionManager.Instance.FinishSessionAsync(SessionStatus.Completed);
```

## Configuración

Se define en un asset `BitacoraConfig` (**Create → Bitácora → Configuración**), asignado al componente `AppManager` del objeto `Bitacora`. Se puede tener uno por compilación, por ejemplo uno para una demo y otro por cliente.

| Campo | Descripción |
|---|---|
| `Backend` | `Mock` (simulado) o `Http` (servidor real) |
| `Api Base Url` | URL del servidor |
| `Api Key` | Clave enviada en cada solicitud |
| `Simulation Id` | Identificador de la simulación |
| `Request Timeout Seconds` | Tiempo máximo de espera de cada solicitud |
| `Participant Mode` | `Anonymous`, `Email`, `FullName` o `DocumentId` |
| `Batch Size` | Cantidad de eventos que dispara un envío |
| `Flush Interval Seconds` | Segundos entre envíos, aunque no se complete el lote |
| `Reconnect Interval Seconds` | Segundos entre reintentos de conexión |
| `Quit Timeout Seconds` | Tiempo máximo para cerrar la sesión al salir |
| `Mock Latency Seconds` | Demora simulada de cada respuesta (backend simulado) |
| `Mock Simulate Offline` | Simula la falta de conexión; se puede cambiar durante el modo Play |

La configuración incluida en una compilación es visible para cualquiera que la inspeccione: no incluir claves secretas.

## Componentes

| Componente | Responsabilidad |
|---|---|
| `AppManager` | Inicialización, configuración y estado de conexión |
| `SessionManager` | Sesión activa: creación, alias, pausas y cierre |
| `EventManager` | Registro de eventos, numeración, cola y envío por lotes |
| `IBitacoraBackend` | Contrato con el servidor |
| `MockBackend` | Backend simulado |
| `HttpBackend` | Backend real, mediante `UnityWebRequest` |
| `ConnectionStatusView` | Componente de interfaz que muestra el estado de conexión |

Los componentes del núcleo se agrupan en el objeto `Bitacora`, que debe estar en la raíz de la primera escena y persiste entre escenas (`DontDestroyOnLoad`). Los componentes de interfaz pertenecen a cada escena y leen el estado del núcleo.

## Escenas de ejemplo

| Escena | Contenido |
|---|---|
| `SampleHome` | Título, campo de participante (según el modo), botón Comenzar y estado de conexión |
| `SampleSimulation` | Cuatro botones de eventos de prueba (A, B, C, D) y botón Finalizar sesión |

## Contrato con el servidor

El formato de sesiones, eventos y errores se documenta en [`API.md`](Packages/com.christiantoledo.bitacora/Documentation~/API.md).

## Servidor de prueba

La carpeta [`test-server/`](test-server/) contiene un servidor local en Flask que implementa el contrato, para probar el backend real sin un servidor de producción. Permite simular fallas y demoras. Ver su [README](test-server/README.md).

## Limitaciones conocidas

- En el editor, detener el modo Play no cierra la sesión: el cierre automático funciona en compilaciones.
- En el navegador, cerrar la pestaña no cierra la sesión.

## Estructura del repositorio

El repositorio es un proyecto de Unity que contiene el package y sirve para desarrollarlo y probarlo.

```
Assets/Samples/Template/                 # Escenas de ejemplo durante el desarrollo
Packages/
└── com.christiantoledo.bitacora/        # El package
    ├── package.json
    ├── Runtime/                         # Código del package
    ├── Samples~/Template/               # Escenas de ejemplo que se distribuyen
    └── Documentation~/API.md            # Contrato con el servidor
test-server/                             # Servidor de prueba (Flask)
ProjectSettings/
```

## Estado

Versión 0.1.0: flujo completo con backend simulado y real.

Próximamente:

- Almacenamiento local de eventos cuando no hay conexión.
- Cierre de la sesión al cerrar la pestaña del navegador.
- Vinculación de dispositivos con código.
- Integración con XR.

## Licencia

[MIT](LICENSE)