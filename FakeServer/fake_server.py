"""Local stand-in for the DTA and legacy pronunciation servers, for testing the app.

Standard library only. It returns sample responses shaped like the ones the Unity client
parses, and it is not a copy of the real servers. Guide: docs/NEW/FAKE_SERVER.md

    python FakeServer/fake_server.py                 start on http://127.0.0.1:8765
    python FakeServer/fake_server.py --print-secret  print the lines for Secret.cs
"""

import argparse
import json
import random
import re
import threading
import time
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import parse_qs, parse_qsl, urlsplit

API = "/api/v1"
LEVELS = ("A1", "A2", "B1", "B2", "C1_plus")
ROUTES = {
    ("POST", f"{API}/speech/assess"): "assess",
    ("POST", f"{API}/onboarding"): "onboarding",
    ("POST", f"{API}/feedback"): "feedback",
    ("POST", f"{API}/analytics/comparison"): "profile",
    ("PATCH", f"{API}/users/level"): "level",
    ("POST", f"{API}/events"): "events",
    ("DELETE", f"{API}/users"): "delete",
    ("POST", "/asr_unity"): "mdd",
    ("POST", "/asr_number"): "number",
}
RELEVANCE = ("on_topic", "partial", "off_topic", "none")
PROFILES = ("ok", "top", "bottom", "insufficient", "cohort", "unavailable")

# Seconds a response takes, so loading states stay on screen long enough to be seen.
DEFAULT_DELAY = (2.0, 5.0)

# Feedback about one recording must name it, and other feedback must not (the server's 422).
ASSESSMENT_FEEDBACK = {"self_assessment", "result_accuracy", "result_understanding"}
GENERAL_FEEDBACK = {"comparison_ui", "overall_experience"}

ERROR_TYPES = {
    400: "BAD_REQUEST",
    403: "FORBIDDEN",
    404: "USER_NOT_FOUND",
    409: "CONFLICT",
    413: "PAYLOAD_TOO_LARGE",
    415: "UNSUPPORTED_MEDIA_TYPE",
    429: "RATE_LIMITED",
    500: "INTERNAL_ERROR",
    503: "SERVICE_UNAVAILABLE",
}


class ApiError(Exception):
    """A failed request, sent in the server's error format."""

    def __init__(self, status, message):
        super().__init__(message)
        self.status = status
        self.message = message


class State:
    """What the server remembers and how it should behave. Held in memory only."""

    def __init__(self, delete_key="fake-delete-key", strict_users=False):
        self.lock = threading.Lock()
        self.delete_key = delete_key
        self.strict_users = strict_users
        self.users = {}
        self.next_assessment_id = 1
        self.events_received = 0
        self.fail = {}
        self.delay = DEFAULT_DELAY
        self.relevance = "on_topic"
        self.profile = "ok"
        self.warning = []

    def settings(self):
        return {
            "fail": self.fail,
            "delay": self.delay,
            "relevance": self.relevance,
            "profile": self.profile,
            "warning": self.warning,
            "strict_users": self.strict_users,
            "users": len(self.users),
            "events_received": self.events_received,
        }

    def apply(self, query):
        """Applies settings from a control URL, checking all of them before changing any."""
        changes = {}
        for key, value in query.items():
            if key == "fail":
                changes[key] = parse_fail(value)
            elif key == "delay":
                changes[key] = parse_delay(value)
            elif key == "relevance":
                changes[key] = choice(value, RELEVANCE, key)
            elif key == "profile":
                changes[key] = choice(value, PROFILES, key)
            elif key == "warning":
                changes[key] = [int(w) for w in value.split(",") if w and w != "none"]
            elif key == "reset":
                changes.update(
                    fail={},
                    delay=DEFAULT_DELAY,
                    relevance="on_topic",
                    profile="ok",
                    warning=[],
                )
            else:
                raise ValueError(f"Unknown setting: {key}")
        for key, value in changes.items():
            setattr(self, key, value)


def choice(value, allowed, name):
    if value not in allowed:
        raise ValueError(f"{name} must be one of {', '.join(allowed)}")
    return value


def parse_fail(text):
    """Reads 'assess:503,profile:500' into a route-to-status map. 'none' clears it."""
    if text in ("", "none"):
        return {}
    known = set(ROUTES.values())
    fail = {}
    for item in text.split(","):
        route, _, status = item.partition(":")
        if route not in known:
            raise ValueError(f"Unknown route {route!r}. Routes: {', '.join(sorted(known))}")
        fail[route] = int(status)
    return fail


def parse_delay(text):
    """Reads '2-5' as a range of seconds, or a single number such as '0' as a fixed wait."""
    low, _, high = text.partition("-")
    low = float(low)
    high = float(high) if high else low
    if low < 0 or high < low:
        raise ValueError("delay must be a number or a range such as 2-5")
    return low, high


def parse_form(content_type, body):
    """Reads a WWWForm body. Returns its text fields and its files, as name -> (filename, bytes)."""
    if "multipart/form-data" not in content_type:
        pairs = parse_qsl(body.decode("utf-8"), keep_blank_values=True)
        return dict(pairs), {}

    match = re.search(r'boundary="?([^";]+)"?', content_type)
    if not match:
        raise ApiError(400, "Multipart body has no boundary.")

    fields, files = {}, {}
    for part in body.split(b"--" + match.group(1).encode("latin-1")):
        head, separator, content = part.partition(b"\r\n\r\n")
        if not separator:
            continue
        headers = head.decode("utf-8", "replace")
        name = re.search(r'(?<![a-z])name="([^"]*)"', headers)
        if not name:
            continue
        content = content[:-2] if content.endswith(b"\r\n") else content
        filename = re.search(r'filename="([^"]*)"', headers)
        if filename:
            files[name.group(1)] = (filename.group(1), content)
        else:
            fields[name.group(1)] = content.decode("utf-8", "replace")
    return fields, files


def require_user(state, fields):
    """Finds the user. Unknown guids are accepted unless --strict-users is on, so restarting
    this server, which forgets everyone, does not force every test device to onboard again."""
    guid = fields.get("guid", "")
    if not guid:
        raise ApiError(400, "guid is required.")
    if guid not in state.users:
        if state.strict_users:
            raise ApiError(404, "No user with this guid.")
        state.users[guid] = {"level": "A2", "onboarded": False}
    return state.users[guid]


def fine_label(score):
    """Floored into the only four bands the server sends."""
    if score < 2:
        return "A1"
    if score < 2.5:
        return "A2"
    if score < 3:
        return "A2+"
    return "B1"


def coarse_label(score):
    return fine_label(score).rstrip("+")


def label(score):
    return {"label": coarse_label(score), "label_fine": fine_label(score)}


def handle_assess(state, fields, files):
    require_user(state, fields)

    audio = files.get("file")
    if audio is None:
        raise ApiError(400, "file is required.")
    if not audio[0].endswith(".wav"):
        raise ApiError(400, f"Filename must end in .wav: {audio[0]}")

    task_id = fields.get("task_id", "")
    if task_id not in ("1", "2", "3", "4", "5", "6"):
        raise ApiError(400, f"Unknown task_id {task_id}")
    task_id = int(task_id)

    # A different band for each task, so all the star rows can be seen by changing task.
    base = {1: 1.6, 2: 2.2, 3: 2.7, 4: 3.1, 5: 3.6, 6: 2.4}[task_id]
    scores = {
        "accuracy": round(base + 0.1, 2),
        "fluency": round(base - 0.3, 2),
        "proficiency": base,
        "pronunciation": round(base + 0.3, 2),
        "range": round(base - 0.1, 2),
    }

    result = {
        "transcript": "Tämä on testivastaus paikalliselta palvelimelta.",
        "scores": scores,
        "assessment_id": state.next_assessment_id,
        "task_id": task_id,
        "cefr_label": coarse_label(base),
        "cefr_label_fine": fine_label(base),
        "dimension_labels": {name: label(scores[name]) for name in ("accuracy", "fluency", "pronunciation", "range")},
        "clipped": False,
    }
    if state.relevance != "none":
        result["content"] = {
            "relevance": state.relevance,
            "confidence": 0.9,
            "reason": f"Fake server: {state.relevance}",
            "judge": "fake",
        }

    state.next_assessment_id += 1
    return 200, result


def handle_onboarding(state, fields, files):
    guid = fields.get("guid", "")
    if not guid:
        raise ApiError(400, "guid is required.")

    existing = state.users.get(guid)
    if existing and existing["onboarded"]:
        raise ApiError(409, "This guid is already registered.")

    state.users[guid] = {"level": "A2", "onboarded": True}
    return 200, {"guid": guid, "status": "created"}


def handle_feedback(state, fields, files):
    require_user(state, fields)

    kind = fields.get("feedback_classification", "")
    if kind not in ASSESSMENT_FEEDBACK | GENERAL_FEEDBACK:
        raise ApiError(422, f"Unknown feedback_classification: {kind}")
    if fields.get("reaction_value") not in ("1", "2", "3", "4", "5"):
        raise ApiError(422, "reaction_value must be 1 to 5.")

    has_assessment = "assessment_id" in fields
    if kind in ASSESSMENT_FEEDBACK and not has_assessment:
        raise ApiError(422, f"{kind} needs an assessment_id.")
    if kind in GENERAL_FEEDBACK and has_assessment:
        raise ApiError(422, f"{kind} must not send an assessment_id.")

    return 200, {"status": "stored"}


def handle_profile(state, fields, files):
    level = require_user(state, fields)["level"]

    if state.profile == "insufficient":
        return 200, {
            "status": "USER_ASSESSMENT_DATA_INSUFFICIENT",
            "message": "Fake server: not enough assessments.",
            "cefr_level": level,
            "current_assessments": 1,
            "required_assessments": 3,
        }
    if state.profile == "cohort":
        return 200, {
            "status": "COHORT_SIZE_TOO_SMALL",
            "message": "Fake server: cohort too small.",
            "cefr_level": level,
            "cohort_size": 4,
        }
    if state.profile == "unavailable":
        return 200, {"status": "RANK_UNAVAILABLE", "message": "Fake server: rank unavailable."}

    # 0.75 is Const.ASA_ADVANCE_MIN_PERCENTILE, so "top" offers Advance and "ok" does not.
    percentile, rank, top_percent, top_rank = {
        "ok": (0.6, 38, 50, 50),
        "top": (0.97, 3, 5, 3),
        "bottom": (0.2, 76, None, None),
    }[state.profile]
    return 200, {
        "percentile": percentile,
        "rank": rank,
        "cefr_level": level,
        "cohort_size": 95,
        "display": {"top_percent": top_percent, "top_rank": top_rank},
    }


def handle_level(state, fields, files):
    user = require_user(state, fields)
    level = fields.get("cefr_level", "")
    if level not in LEVELS:
        raise ApiError(422, f"cefr_level must be one of {', '.join(LEVELS)}")
    user["level"] = level
    return 200, {"cefr_level": level}


def handle_delete(state, fields, files, delete_key):
    if delete_key != state.delete_key:
        raise ApiError(403, "X-Delete-Key did not match.")
    guid = fields.get("guid", "")
    if not guid:
        raise ApiError(400, "guid is required.")
    state.users.pop(guid, None)
    return 204, None


def handle_events(state, body):
    try:
        batch = json.loads(body)
        events = batch["events"]
        for key in ("guid", "asa_session_id", "app_version"):
            batch[key]
        for event in events:
            for key in ("event_id", "occurred_at", "name", "properties"):
                event[key]
    except (ValueError, KeyError, TypeError) as error:
        raise ApiError(400, f"Malformed event batch: {error!r}")
    if len(events) > 100:
        raise ApiError(413, "A batch holds at most 100 events.")

    state.events_received += len(events)
    return 200, {"received": len(events)}


def pronunciation_result(state, text):
    """One score per character, which the client requires, with every colour represented.

    The middle character is scored as wrong and reported as a substitution, so the detail
    screen always has a target and actual phoneme to show.
    """
    middle = len(text) // 2
    score = []
    for i in range(len(text)):
        if i == middle:
            score.append(0.1)
        elif i % 5 == 4:
            score.append(0.55)
        else:
            score.append(0.95)

    substitute = "a" if text[middle : middle + 1] != "a" else "e"
    return {
        "levenshtein": [{"ops": "replace", "tran_index": middle, "pred_index": middle}] if text else [],
        "prediction": text[:middle] + substitute + text[middle + 1 :] if text else "",
        "score": score,
        "warning": state.warning,
    }


def handle_mdd(state, fields, files):
    if "file" not in files or "transcript" not in fields:
        raise ApiError(400, "file and transcript are required.")
    return 200, pronunciation_result(state, fields["transcript"])


def handle_number(state, fields, files):
    if "file" not in files or "target_number" not in fields:
        raise ApiError(400, "file and target_number are required.")
    return 200, pronunciation_result(state, fields["target_number"])


FORM_HANDLERS = {
    "assess": handle_assess,
    "onboarding": handle_onboarding,
    "feedback": handle_feedback,
    "profile": handle_profile,
    "level": handle_level,
    "mdd": handle_mdd,
    "number": handle_number,
}


def error_body(status, message):
    # FastAPI validation errors carry an array here, which is why the client branches on status.
    if status == 422:
        return {"detail": [{"loc": ["body"], "msg": message, "type": "value_error"}]}
    return {"detail": {"type": ERROR_TYPES.get(status, "ERROR"), "message": message}}


class Handler(BaseHTTPRequestHandler):
    state = None
    protocol_version = "HTTP/1.1"

    def do_GET(self):
        self.handle_control()

    def do_POST(self):
        self.handle_api()

    def do_PATCH(self):
        self.handle_api()

    def do_DELETE(self):
        self.handle_api()

    def handle_control(self):
        """Changes behaviour while the app runs, from a browser: /__fake/set?profile=top"""
        url = urlsplit(self.path)
        with self.state.lock:
            try:
                if url.path == "/__fake/stop":
                    self.reply(200, {"stopping": True})
                    # On another thread, because shutdown waits for this request to finish.
                    threading.Thread(target=self.server.shutdown, daemon=True).start()
                    return
                if url.path == "/__fake/set":
                    query = {k: v[0] for k, v in parse_qs(url.query, keep_blank_values=True).items()}
                    self.state.apply(query)
                elif url.path not in ("/", "/__fake", "/__fake/"):
                    self.reply(404, {"detail": {"type": "NOT_FOUND", "message": url.path}})
                    return
            except ValueError as error:
                self.reply(400, {"error": str(error)})
                return
            self.reply(200, self.state.settings())

    def handle_api(self):
        length = int(self.headers.get("Content-Length") or 0)
        body = self.rfile.read(length)
        route = ROUTES.get((self.command, urlsplit(self.path).path))

        if route is None:
            self.reply(404, {"detail": {"type": "NOT_FOUND", "message": self.path}})
            return

        # Outside the lock, so a slow response does not hold up other requests.
        wait = random.uniform(*self.state.delay)
        if wait:
            time.sleep(wait)

        with self.state.lock:
            try:
                status, payload = self.dispatch(route, body)
            except ApiError as error:
                status, payload = error.status, error_body(error.status, error.message)
            self.reply(status, payload)

    def dispatch(self, route, body):
        forced = self.state.fail.get(route)
        if forced:
            raise ApiError(forced, f"Fake server: {route} set to fail with {forced}.")

        if route == "events":
            return handle_events(self.state, body)

        content_type = self.headers.get("Content-Type", "")
        fields, files = parse_form(content_type, body)

        if route == "delete":
            return handle_delete(self.state, fields, files, self.headers.get("X-Delete-Key", ""))
        return FORM_HANDLERS[route](self.state, fields, files)

    def reply(self, status, payload):
        data = b"" if payload is None else json.dumps(payload, ensure_ascii=False).encode("utf-8")
        self.send_response(status)
        if payload is not None:
            self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(data)))
        self.end_headers()
        self.wfile.write(data)

    def log_message(self, format, *args):
        print(f"{self.log_date_time_string()}  {format % args}", flush=True)


def make_server(host, port, state):
    handler = type("BoundHandler", (Handler,), {"state": state})
    return ThreadingHTTPServer((host, port), handler)


def secret_lines(base_url, delete_key):
    urls = {
        "ASA_URL": f"{API}/speech/assess",
        "ASA_CONSENT_URL": f"{API}/onboarding",
        "USER_ASA_FEEDBACK_URL": f"{API}/feedback",
        "ASA_PROFILE_URL": f"{API}/analytics/comparison",
        "ASA_SET_LEVEL_URL": f"{API}/users/level",
        "ASA_EVENTS_URL": f"{API}/events",
        "DATA_DEL_REQUEST_URL": f"{API}/users",
        "AUDIO_URL": "/asr_unity",
        "NUMBER_AUDIO_URL": "/asr_number",
    }
    lines = [f'public const string {name} = "{base_url}{path}";' for name, path in urls.items()]
    lines.append(f'public const string SERVER_DELETE_KEY = "{delete_key}";')
    return "\n".join(lines)


def parse_args():
    parser = argparse.ArgumentParser(description="Local fake server for testing the app.")
    parser.add_argument("--host", default="127.0.0.1", help="use 0.0.0.0 to accept a phone on your network")
    parser.add_argument("--port", type=int, default=8765)
    parser.add_argument("--public-host", help="address --print-secret uses, such as your computer's IP")
    parser.add_argument("--print-secret", action="store_true", help="print the lines for Secret.cs and exit")
    parser.add_argument("--delete-key", default="fake-delete-key")
    parser.add_argument("--strict-users", action="store_true", help="answer 404 for guids that have not onboarded")
    parser.add_argument("--fail", default="", help="for example assess:503,profile:500")
    parser.add_argument("--delay", default="2-5", help="seconds to wait before each response, such as 2-5 or 0")
    parser.add_argument("--relevance", choices=RELEVANCE, default="on_topic")
    parser.add_argument("--profile", choices=PROFILES, default="ok")
    return parser.parse_args()


def main():
    args = parse_args()

    if args.print_secret:
        print(secret_lines(f"http://{args.public_host or args.host}:{args.port}", args.delete_key))
        return

    state = State(args.delete_key, args.strict_users)
    state.apply(
        {
            "fail": args.fail,
            "delay": args.delay,
            "relevance": args.relevance,
            "profile": args.profile,
        }
    )

    server = make_server(args.host, args.port, state)
    print(f"Fake server on http://{args.host}:{args.port}  (Ctrl+C to stop)", flush=True)
    print(f"Settings and controls: http://127.0.0.1:{args.port}/__fake/", flush=True)
    if args.host != "127.0.0.1":
        print("Reachable from your network. It has no authentication, so stop it when finished.", flush=True)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        server.server_close()


if __name__ == "__main__":
    main()
