"""
Servidor de prueba de Bitácora.

Implementa el contrato definido en Packages/com.christiantoledo.bitacora/Documentation~/API.md
para probar HttpBackend en local. Guarda cada sesión en un archivo JSON dentro de data/.

No es un servidor de producción: no usa base de datos ni está pensado para muchos usuarios.
"""

import json
import os
import random
import re
import threading
import time
import uuid
from datetime import datetime, timezone
from functools import wraps
from pathlib import Path

from flask import Flask, jsonify, request
from flask_cors import CORS

# --- Configuración (variables de entorno) ------------------------------------

API_KEY = os.environ.get("BITACORA_API_KEY", "dev-key")
HOST = os.environ.get("BITACORA_HOST", "127.0.0.1")
PORT = int(os.environ.get("BITACORA_PORT", "8000"))
FAIL_RATE = float(os.environ.get("BITACORA_FAIL_RATE", "0"))      # 0 a 1
LATENCY = float(os.environ.get("BITACORA_LATENCY", "0"))          # segundos
ORIGINS = os.environ.get("BITACORA_ORIGINS", "*")                 # CORS
DATA_DIR = Path(os.environ.get("BITACORA_DATA_DIR", Path(__file__).parent / "data"))

# --- Reglas del contrato -------------------------------------------------------

MAX_BATCH = 100
MAX_TYPE_LENGTH = 64
MAX_DATA_BYTES = 4096
MAX_PARTICIPANT_LENGTH = 200
PARTICIPANT_TYPES = {"email", "full_name", "document_id"}
FINISH_STATUSES = {"completed", "abandoned"}
TYPE_PATTERN = re.compile(r"^[a-z][a-z0-9_]*$")

ANIMALS = ["Carpincho", "Hornero", "Yaguareté", "Ñandú", "Guanaco", "Tatú", "Pingüino",
           "Puma", "Zorro", "Cóndor", "Vicuña", "Mara", "Coatí", "Tero", "Benteveo", "Lechuza"]
# Adjetivos y colores que no cambian con el género del animal
QUALITIES = ["Turquesa", "Violeta", "Celeste", "Naranja", "Verde", "Azul", "Gris", "Rosa",
             "Veloz", "Valiente", "Audaz", "Amable", "Feliz", "Paciente", "Alegre", "Sutil"]

# --- Aplicación ------------------------------------------------------------------

app = Flask(__name__)
app.json.ensure_ascii = False  # alias con tildes y eñes legibles
CORS(app, resources={r"/v1/*": {"origins": ORIGINS}}, allow_headers=["Content-Type", "X-Api-Key"])

SESSIONS_DIR = DATA_DIR / "sessions"
SESSIONS_DIR.mkdir(parents=True, exist_ok=True)

lock = threading.Lock()


def now_iso():
    return datetime.now(timezone.utc).isoformat(timespec="milliseconds").replace("+00:00", "Z")


def error(status, code, message):
    return jsonify({"error": {"code": code, "message": message}}), status


def session_path(session_id):
    return SESSIONS_DIR / f"{session_id}.json"


def load_session(session_id):
    # Evita rutas manipuladas: el identificador debe ser un UUID
    try:
        uuid.UUID(session_id)
    except ValueError:
        return None

    path = session_path(session_id)
    if not path.exists():
        return None
    return json.loads(path.read_text(encoding="utf-8"))


def save_session(session):
    path = session_path(session["session_id"])
    temp = path.with_suffix(".tmp")
    temp.write_text(json.dumps(session, ensure_ascii=False, indent=2), encoding="utf-8")
    temp.replace(path)  # reemplazo atómico: nunca queda un archivo a medio escribir


def used_aliases():
    aliases = set()
    for path in SESSIONS_DIR.glob("*.json"):
        try:
            aliases.add(json.loads(path.read_text(encoding="utf-8"))["alias"])
        except (json.JSONDecodeError, KeyError):
            continue
    return aliases


def new_alias():
    used = used_aliases()
    combinations = [f"{a} {q}" for a in ANIMALS for q in QUALITIES]
    free = [c for c in combinations if c not in used]
    if free:
        return random.choice(free)
    # Combinaciones agotadas: se agrega un número
    while True:
        candidate = f"{random.choice(combinations)} {random.randint(2, 99)}"
        if candidate not in used:
            return candidate


def is_iso_utc(value):
    if not isinstance(value, str) or not value.endswith("Z"):
        return False
    try:
        datetime.fromisoformat(value.replace("Z", "+00:00"))
        return True
    except ValueError:
        return False


# --- Middleware ------------------------------------------------------------------

def requires_api_key(view):
    @wraps(view)
    def wrapper(*args, **kwargs):
        if request.headers.get("X-Api-Key") != API_KEY:
            return error(401, "invalid_api_key", "Clave ausente o incorrecta.")
        return view(*args, **kwargs)
    return wrapper


@app.before_request
def simulate_conditions():
    if request.method == "OPTIONS" or not request.path.startswith("/v1/"):
        return None
    if LATENCY > 0:
        time.sleep(LATENCY)
    if FAIL_RATE > 0 and random.random() < FAIL_RATE:
        app.logger.warning("Falla simulada en %s %s", request.method, request.path)
        return error(503, "server_error", "Falla simulada del servidor.")
    return None


# --- Endpoints -----------------------------------------------------------------------

@app.get("/v1/health")
@requires_api_key
def health():
    return jsonify({"status": "ok"})


@app.post("/v1/sessions")
@requires_api_key
def create_session():
    body = request.get_json(silent=True)
    if not isinstance(body, dict):
        return error(400, "invalid_request", "El cuerpo debe ser un objeto JSON.")

    simulation_id = body.get("simulation_id")
    if not isinstance(simulation_id, str) or not simulation_id.strip():
        return error(400, "invalid_request", "Falta simulation_id.")

    participant = body.get("participant")
    if participant is not None:
        if (not isinstance(participant, dict)
                or participant.get("type") not in PARTICIPANT_TYPES
                or not isinstance(participant.get("value"), str)
                or not participant["value"].strip()
                or len(participant["value"]) > MAX_PARTICIPANT_LENGTH):
            return error(400, "invalid_request",
                         "participant debe ser null o { type, value } con un tipo válido.")

    with lock:
        session = {
            "session_id": str(uuid.uuid4()),
            "alias": new_alias(),
            "simulation_id": simulation_id.strip(),
            "participant": participant,
            "started_at": now_iso(),
            "status": "active",
            "finished_at": None,
            "events": [],
        }
        save_session(session)

    app.logger.info("Sesión creada: %s (%s)", session["session_id"], session["alias"])
    return jsonify({
        "session_id": session["session_id"],
        "alias": session["alias"],
        "started_at": session["started_at"],
    }), 201


@app.post("/v1/sessions/<session_id>/events")
@requires_api_key
def add_events(session_id):
    body = request.get_json(silent=True)
    if not isinstance(body, dict) or not isinstance(body.get("events"), list):
        return error(400, "invalid_request", "El cuerpo debe tener una lista events.")

    events = body["events"]
    if len(events) > MAX_BATCH:
        return error(413, "batch_too_large", f"Un lote admite hasta {MAX_BATCH} eventos.")

    for index, event in enumerate(events):
        problem = validate_event(event)
        if problem:
            return error(400, "invalid_request", f"Evento {index}: {problem}")

    with lock:
        session = load_session(session_id)
        if session is None:
            return error(404, "session_not_found", "La sesión no existe.")
        if session["status"] != "active":
            return error(409, "session_finished", "La sesión ya fue cerrada.")

        known = {e["sequence"] for e in session["events"]}
        accepted = 0
        duplicates = 0

        for event in events:
            if event["sequence"] in known:
                duplicates += 1
                continue
            known.add(event["sequence"])
            session["events"].append({
                "sequence": event["sequence"],
                "timestamp": event["timestamp"],
                "type": event["type"],
                "data": event.get("data"),
                "received_at": now_iso(),
            })
            accepted += 1

        session["events"].sort(key=lambda e: e["sequence"])
        save_session(session)

    app.logger.info("Sesión %s: %d eventos aceptados, %d duplicados", session_id, accepted, duplicates)
    return jsonify({"accepted": accepted, "duplicates": duplicates})


def validate_event(event):
    if not isinstance(event, dict):
        return "debe ser un objeto."
    sequence = event.get("sequence")
    if not isinstance(sequence, int) or isinstance(sequence, bool) or sequence < 1:
        return "sequence debe ser un entero desde 1."
    if not is_iso_utc(event.get("timestamp")):
        return "timestamp debe estar en ISO 8601 UTC."
    event_type = event.get("type")
    if (not isinstance(event_type, str) or len(event_type) > MAX_TYPE_LENGTH
            or not TYPE_PATTERN.match(event_type)):
        return f"type debe estar en snake_case, hasta {MAX_TYPE_LENGTH} caracteres."
    data = event.get("data")
    if data is not None:
        if not isinstance(data, dict):
            return "data debe ser un objeto o null."
        if len(json.dumps(data).encode("utf-8")) > MAX_DATA_BYTES:
            return f"data supera los {MAX_DATA_BYTES} bytes."
    return None


@app.post("/v1/sessions/<session_id>/finish")
@requires_api_key
def finish_session(session_id):
    body = request.get_json(silent=True)
    status = body.get("status") if isinstance(body, dict) else None
    if status not in FINISH_STATUSES:
        return error(400, "invalid_request", "status debe ser completed o abandoned.")

    with lock:
        session = load_session(session_id)
        if session is None:
            return error(404, "session_not_found", "La sesión no existe.")

        # Cerrar una sesión ya cerrada devuelve los datos del primer cierre
        if session["status"] == "active":
            session["status"] = status
            session["finished_at"] = now_iso()
            save_session(session)
            app.logger.info("Sesión %s cerrada: %s", session_id, status)

    return jsonify({
        "session_id": session["session_id"],
        "status": session["status"],
        "finished_at": session["finished_at"],
    })


# --- Errores generales ---------------------------------------------------------

@app.errorhandler(404)
def not_found(_):
    return error(404, "not_found", "La ruta no existe.")


@app.errorhandler(405)
def method_not_allowed(_):
    return error(405, "method_not_allowed", "Método no permitido en esta ruta.")


if __name__ == "__main__":
    import logging
    logging.basicConfig(level=logging.INFO)
    print(f"Servidor de prueba de Bitácora en http://{HOST}:{PORT}  (clave: {API_KEY})")
    app.run(host=HOST, port=PORT, debug=False, threaded=True)