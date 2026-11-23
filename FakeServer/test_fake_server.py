"""Tests the fake server over real HTTP, sending the same body formats as Unity's WWWForm.

python -B FakeServer/test_fake_server.py
"""

import json
import threading
import unittest
import urllib.error
import urllib.parse
import urllib.request
from unittest import mock

from fake_server import API, DEFAULT_DELAY, ROUTES, State, make_server, secret_lines

BOUNDARY = "testBoundary123"


def multipart(fields, filename="answer.wav"):
    """A body laid out like WWWForm.data when it holds binary data."""
    out = b""
    for name, value in fields.items():
        out += (
            f'--{BOUNDARY}\r\nContent-Type: text/plain; charset="utf-8"\r\n'
            f'Content-disposition: form-data; name="{name}"\r\n\r\n{value}\r\n'
        ).encode("utf-8")
    out += (
        f"--{BOUNDARY}\r\nContent-Type: audio/wav\r\n"
        f'Content-disposition: form-data; name="file"; filename="{filename}"\r\n\r\n'
    ).encode("utf-8")
    out += b"RIFF\r\n--not-a-boundary\x00\xff" + f"\r\n--{BOUNDARY}--\r\n".encode("utf-8")
    return out


class FakeServerTest(unittest.TestCase):
    def setUp(self):
        self.state = State()
        self.state.delay = (0.0, 0.0)  # Tests are not waiting on loading states.
        self.server = make_server("127.0.0.1", 0, self.state)
        self.base = f"http://127.0.0.1:{self.server.server_address[1]}"
        self.server.RequestHandlerClass.log_message = lambda *args: None
        self.thread = threading.Thread(target=self.server.serve_forever, daemon=True)
        self.thread.start()

    def tearDown(self):
        self.server.shutdown()
        self.server.server_close()

    def request(self, method, path, body=None, content_type=None, headers=None):
        request = urllib.request.Request(self.base + path, data=body, method=method)
        if content_type:
            request.add_header("Content-Type", content_type)
        for key, value in (headers or {}).items():
            request.add_header(key, value)
        try:
            with urllib.request.urlopen(request) as response:
                return response.status, response.read()
        except urllib.error.HTTPError as error:
            with error:
                return error.code, error.read()

    def form(self, method, path, fields, headers=None):
        body = urllib.parse.urlencode(fields).encode("utf-8")
        status, data = self.request(method, path, body, "application/x-www-form-urlencoded", headers)
        return status, json.loads(data) if data else None

    def upload(self, path, fields, filename="answer.wav"):
        status, data = self.request(
            "POST", path, multipart(fields, filename), f'multipart/form-data; boundary="{BOUNDARY}"'
        )
        return status, json.loads(data)

    def control(self, query):
        status, data = self.request("GET", f"/__fake/set?{query}")
        return status, json.loads(data)

    def test_pronunciation_scores_match_transcript_length(self):
        status, result = self.upload("/asr_unity", {"transcript": "mustikkapäivä"})
        self.assertEqual(status, 200)
        self.assertEqual(len(result["score"]), len("mustikkapäivä"))
        self.assertEqual(len(result["prediction"]), len("mustikkapäivä"))
        operation = result["levenshtein"][0]
        self.assertEqual(operation["ops"], "replace")
        self.assertNotEqual(result["prediction"][operation["pred_index"]], "mustikkapäivä"[operation["tran_index"]])

    def test_number_game_scores_match_number_length(self):
        status, result = self.upload("/asr_number", {"target_number": "18"})
        self.assertEqual(status, 200)
        self.assertEqual(len(result["score"]), 2)

    def test_assess_echoes_task_and_uses_known_bands(self):
        for task in range(1, 7):
            status, result = self.upload(f"{API}/speech/assess", {"guid": "g", "task_id": task})
            self.assertEqual(status, 200)
            self.assertEqual(result["task_id"], task)
            self.assertIn(result["cefr_label_fine"], ("A1", "A2", "A2+", "B1"))
            self.assertNotIn("+", result["cefr_label"])
            self.assertEqual(set(result["dimension_labels"]), {"accuracy", "fluency", "pronunciation", "range"})
        self.assertEqual(result["assessment_id"], 6)

    def test_assess_rejects_what_the_real_server_rejects(self):
        status, result = self.upload(f"{API}/speech/assess", {"guid": "g", "task_id": 0})
        self.assertEqual((status, result["detail"]["message"]), (400, "Unknown task_id 0"))
        status, _ = self.upload(f"{API}/speech/assess", {"guid": "g", "task_id": 1}, filename="answer")
        self.assertEqual(status, 400)

    def test_relevance_can_be_changed_or_removed(self):
        self.control("relevance=off_topic")
        _, result = self.upload(f"{API}/speech/assess", {"guid": "g", "task_id": 1})
        self.assertEqual(result["content"]["relevance"], "off_topic")
        self.control("relevance=none")
        _, result = self.upload(f"{API}/speech/assess", {"guid": "g", "task_id": 1})
        self.assertNotIn("content", result)

    def test_second_onboarding_conflicts(self):
        path = f"{API}/onboarding"
        self.assertEqual(self.form("POST", path, {"guid": "g"})[0], 200)
        self.assertEqual(self.form("POST", path, {"guid": "g"})[0], 409)

    def test_strict_users_refuses_unknown_guid(self):
        path = f"{API}/analytics/comparison"
        self.assertEqual(self.form("POST", path, {"guid": "g"})[0], 200)
        self.state.strict_users = True
        self.assertEqual(self.form("POST", path, {"guid": "new"})[0], 404)

    def test_feedback_assessment_id_rules(self):
        path = f"{API}/feedback"
        base = {"guid": "g", "reaction_value": 3, "comment": ""}
        scoped = {**base, "feedback_classification": "self_assessment"}
        general = {**base, "feedback_classification": "overall_experience"}
        self.assertEqual(self.form("POST", path, {**scoped, "assessment_id": 1})[0], 200)
        self.assertEqual(self.form("POST", path, general)[0], 200)

        status, result = self.form("POST", path, scoped)
        self.assertEqual(status, 422)
        self.assertIsInstance(result["detail"], list)
        self.assertEqual(self.form("POST", path, {**general, "assessment_id": 1})[0], 422)

    def test_profile_scenarios(self):
        path = f"{API}/analytics/comparison"
        _, result = self.form("POST", path, {"guid": "g"})
        self.assertNotIn("status", result)
        self.assertLess(result["percentile"], 0.75)

        self.control("profile=top")
        self.assertGreaterEqual(self.form("POST", path, {"guid": "g"})[1]["percentile"], 0.75)

        self.control("profile=bottom")
        self.assertIsNone(self.form("POST", path, {"guid": "g"})[1]["display"]["top_rank"])

        expected = {
            "insufficient": "USER_ASSESSMENT_DATA_INSUFFICIENT",
            "cohort": "COHORT_SIZE_TOO_SMALL",
            "unavailable": "RANK_UNAVAILABLE",
        }
        for name, status in expected.items():
            self.control(f"profile={name}")
            self.assertEqual(self.form("POST", path, {"guid": "g"})[1]["status"], status)
        self.assertNotIn("cefr_level", self.form("POST", path, {"guid": "g"})[1])

    def test_level_change_reaches_profile(self):
        self.assertEqual(self.form("PATCH", f"{API}/users/level", {"guid": "g", "cefr_level": "B1"})[0], 200)
        self.assertEqual(self.form("POST", f"{API}/analytics/comparison", {"guid": "g"})[1]["cefr_level"], "B1")
        self.assertEqual(self.form("PATCH", f"{API}/users/level", {"guid": "g", "cefr_level": "Z9"})[0], 422)

    def test_delete_needs_the_key_and_returns_no_body(self):
        self.form("POST", f"{API}/onboarding", {"guid": "g"})
        path = f"{API}/users"
        self.assertEqual(self.form("DELETE", path, {"guid": "g"}, {"X-Delete-Key": "wrong"})[0], 403)
        self.assertEqual(self.form("DELETE", path, {"guid": "g"}, {"X-Delete-Key": "fake-delete-key"}), (204, None))
        self.assertEqual(self.form("POST", f"{API}/onboarding", {"guid": "g"})[0], 200)

    def test_events(self):
        path = f"{API}/events"
        event = {"event_id": "e", "occurred_at": "t", "name": "n", "properties": {}}
        batch = {"guid": "g", "asa_session_id": "s", "app_version": "1", "events": [event]}
        status, data = self.request("POST", path, json.dumps(batch).encode(), "application/json")
        self.assertEqual((status, json.loads(data)), (200, {"received": 1}))
        self.assertEqual(self.request("POST", path, b"{}", "application/json")[0], 400)

    def test_forced_failure_uses_error_envelope_then_clears(self):
        self.assertEqual(self.control("fail=assess:503")[0], 200)
        status, result = self.upload(f"{API}/speech/assess", {"guid": "g", "task_id": 1})
        self.assertEqual((status, result["detail"]["type"]), (503, "SERVICE_UNAVAILABLE"))

        self.control("reset=1")
        self.assertEqual(self.upload(f"{API}/speech/assess", {"guid": "g", "task_id": 1})[0], 200)

    def test_requests_wait_within_the_range_but_controls_do_not(self):
        self.state.delay = DEFAULT_DELAY
        with mock.patch("fake_server.time.sleep") as sleep:
            self.upload(f"{API}/speech/assess", {"guid": "u", "task_id": "1"})
            sleep.assert_called_once()
            self.assertTrue(DEFAULT_DELAY[0] <= sleep.call_args.args[0] <= DEFAULT_DELAY[1])

            sleep.reset_mock()
            self.form("POST", f"{API}/analytics/comparison", {"guid": "u"})
            sleep.assert_called_once()

            sleep.reset_mock()
            self.control("profile=top")
            sleep.assert_not_called()

    def test_delay_can_be_changed_and_reset(self):
        self.assertEqual(self.control("delay=0")[1]["delay"], [0.0, 0.0])
        self.assertEqual(self.control("delay=1-3")[1]["delay"], [1.0, 3.0])
        self.assertEqual(self.control("delay=5")[1]["delay"], [5.0, 5.0])
        self.assertEqual(self.control("delay=5-2")[0], 400)
        self.assertEqual(self.control("reset=1")[1]["delay"], list(DEFAULT_DELAY))

    def test_bad_control_values_change_nothing(self):
        self.assertEqual(self.control("profile=top&relevance=nope")[0], 400)
        self.assertEqual(self.state.profile, "ok")
        self.assertEqual(self.control("fail=nope:500")[0], 400)

    def test_stop_url_shuts_the_server_down(self):
        self.assertEqual(self.request("GET", "/__fake/stop")[0], 200)
        self.thread.join(5)
        self.assertFalse(self.thread.is_alive())

    def test_unknown_path_is_404(self):
        self.assertEqual(self.request("POST", "/nothing", b"")[0], 404)

    def test_every_route_is_in_secret_lines(self):
        lines = secret_lines("http://x", "k")
        for _, path in ROUTES:
            self.assertIn(f'"http://x{path}"', lines)


if __name__ == "__main__":
    unittest.main()
