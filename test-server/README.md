# Servidor de prueba

Servidor local que implementa el [contrato de Bitácora](../Packages/com.christiantoledo.bitacora/Documentation~/API.md) para probar `HttpBackend` sin un backend real. Guarda cada sesión, con sus eventos, en un archivo JSON dentro de `data/sessions/`.

> Es una herramienta de desarrollo, no un servidor de producción.

## Requisitos

Python 3.11 o superior.

## Puesta en marcha

```bash
cd test-server
python3 -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
python app.py
```

El servidor queda disponible en `http://127.0.0.1:8000`, con la clave `dev-key`.

En la configuración de Bitácora:

| Campo | Valor |
|---|---|
| Backend | `Http` |
| Api Base Url | `http://127.0.0.1:8000` |

## Opciones

Se configuran con variables de entorno al iniciar:

| Variable | Por defecto | Descripción |
|---|---|---|
| `BITACORA_API_KEY` | `dev-key` | Clave que deben enviar las solicitudes |
| `BITACORA_HOST` | `127.0.0.1` | Usar `0.0.0.0` para aceptar conexiones de otros dispositivos de la red (por ejemplo, un visor) |
| `BITACORA_PORT` | `8000` | Puerto |
| `BITACORA_FAIL_RATE` | `0` | Proporción de solicitudes que fallan a propósito, de 0 a 1 |
| `BITACORA_LATENCY` | `0` | Demora agregada a cada respuesta, en segundos |
| `BITACORA_ORIGINS` | `*` | Orígenes permitidos por CORS |

Ejemplo, para probar los reintentos con un 30 % de fallas:

```bash
BITACORA_FAIL_RATE=0.3 python app.py
```

## Prueba rápida

```bash
curl -H "X-Api-Key: dev-key" http://127.0.0.1:8000/v1/health

curl -X POST http://127.0.0.1:8000/v1/sessions \
  -H "X-Api-Key: dev-key" -H "Content-Type: application/json" \
  -d '{"simulation_id": "sample", "participant": null}'
```

## Limpiar los datos

Borrar la carpeta `data/`. Se vuelve a crear al iniciar el servidor.