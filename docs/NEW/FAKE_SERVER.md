# Fake Server

A local stand-in for the DTA and legacy pronunciation servers, so the app can be tested
without a backend. It lives in [`FakeServer/`](../../FakeServer/), outside `Assets`, so Unity
does not import it. It needs Python 3 and nothing else.

**What it cannot prove:** its responses are built from the app's response classes and the
server rules noted in the client's comments, not from the real server. It tests how the app
handles a response, not whether the real server sends that response. Run the
[backend checks](MIGRATION.md#checks-waiting-on-the-backend) against the real server before
release.

## Quick start

1. Print the lines for `Secret.cs`:

   ```bash
   python FakeServer/fake_server.py --print-secret
   ```

2. Replace the matching lines in `Assets/Scripts/Utils/Secret.cs` with them. That file is
   git-ignored, so these values are never committed. Keep any real values somewhere safe first.
3. Start the server, and leave it running while you test:

   ```bash
   python FakeServer/fake_server.py
   ```

4. Press Play. Each request appears in the server's console.

To go back to a real backend, restore your real values in `Secret.cs`.

To test a first launch, use **Tools > Delete All PlayerPrefs** in the Editor. Fake server users
are separate: restarting the server forgets them.

## Running it from Unity

**Tools > Fake Server** opens a window that starts and stops the server and changes its settings
while it runs, so no terminal or browser is needed. It runs the server on the default port, using
the Python command shown in the window. Change that command if `python` is not the right one on
your computer.

A server started from this window stops when Unity closes. One started from a terminal keeps
running, but the window can still change its settings and stop it.

The window warns you if `Secret.cs` does not point at the server.

## Testing scenarios

Change behaviour while the app runs by opening a URL in a browser. The response shows the
current settings, and <http://127.0.0.1:8765/__fake/> shows them without changing anything.

| To test                       | Open `http://127.0.0.1:8765/__fake/set?...`  |
| ----------------------------- | -------------------------------------------- |
| A request failing             | `fail=assess:503`                            |
| Several failures              | `fail=assess:503,profile:500`                |
| How long responses take       | `delay=1-3`, `delay=5` or `delay=0`          |
| Off-topic or partial answers  | `relevance=off_topic` or `relevance=partial` |
| No relevance check ran        | `relevance=none`                             |
| Advance button on the profile | `profile=top`                                |
| No position shown             | `profile=bottom`                             |
| Too few assessments           | `profile=insufficient`                       |
| Cohort too small              | `profile=cohort`                             |
| Rank unavailable              | `profile=unavailable`                        |
| Pronunciation warnings        | `warning=0,1`                                |
| Back to normal                | `reset=1`                                    |

Every request takes 2 to 5 seconds by default, so loading states can be seen. This includes requests
set to fail. Use `delay=0` to turn it off.

To stop the server, open <http://127.0.0.1:8765/__fake/stop>.

Route names for `fail` are `assess`, `onboarding`, `feedback`, `profile`, `level`, `events`,
`delete`, `mdd` and `number`.

The app chooses its error message from the status code, so these are worth trying with
`fail`: `403`, `404`, `413`, `422`, `503`, and `400` or `500` for the server's own wording.
To test **no connection**, stop the server.

Most of these are also start-up options, such as `--profile top` or `--fail assess:503`. Run
`python FakeServer/fake_server.py --help` for the full list.

## Endpoints

| Method and path                     | `Secret.cs` constant    | Response                                                     |
| ----------------------------------- | ----------------------- | ------------------------------------------------------------ |
| `POST /api/v1/speech/assess`        | `ASA_URL`               | Scores and labels. Each task gives a different band.         |
| `POST /api/v1/onboarding`           | `ASA_CONSENT_URL`       | Creates the user, or 409 if the guid exists                  |
| `POST /api/v1/feedback`             | `USER_ASA_FEEDBACK_URL` | Stored, or 422 if `assessment_id` is wrongly sent or missing |
| `POST /api/v1/analytics/comparison` | `ASA_PROFILE_URL`       | Profile stats, depending on `profile`                        |
| `PATCH /api/v1/users/level`         | `ASA_SET_LEVEL_URL`     | Stores the level, which the profile then uses                |
| `POST /api/v1/events`               | `ASA_EVENTS_URL`        | Counts the batch, or 400 if malformed                        |
| `DELETE /api/v1/users`              | `DATA_DEL_REQUEST_URL`  | 204, or 403 if `X-Delete-Key` is wrong                       |
| `POST /asr_unity`                   | `AUDIO_URL`             | One score per character, with one mistake                    |
| `POST /asr_number`                  | `NUMBER_AUDIO_URL`      | The same, for the number                                     |

It does not score or save audio. Results depend only on the task or text sent, so they are
repeatable. `GPT_TTS` calls OpenAI directly and is not covered.

## Remembering users

Users are held in memory, so restarting the server forgets them. An unknown guid is accepted
by default, so a restart does not force the Editor through onboarding again. Use
`--strict-users` to get the real server's 404 for a user who has not onboarded.

## Testing on a phone

1. Start the server so your network can reach it:

   ```bash
   python FakeServer/fake_server.py --host 0.0.0.0
   ```

2. Print the `Secret.cs` lines using your computer's address, then rebuild the app:

   ```bash
   python FakeServer/fake_server.py --print-secret --public-host 192.168.1.20
   ```

3. Allow the port through your firewall if needed. The server has no authentication, so stop
   it when you finish.

## Troubleshooting

| Problem                                          | Likely cause and fix                                                                                                                                                                                                  |
| ------------------------------------------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Insecure connection not allowed` in the Console | The project blocks plain HTTP. In **Player Settings > Other Settings**, set **Allow downloads over HTTP** to **Allowed in development builds**. This changes `ProjectSettings.asset`, so decide whether to commit it. |
| A request throws, or a spinner never stops       | The URL in `Secret.cs` is blank. `UnityWebRequest` throws on an empty URL rather than reporting a failed request.                                                                                                     |
| Every request shows a connection error           | The server is not running, or the port differs from `Secret.cs`.                                                                                                                                                      |
| `Address already in use` when starting           | Another copy is running. Stop it or use `--port`, then print the `Secret.cs` lines again.                                                                                                                             |

## Tests

```bash
cd FakeServer
python -B test_fake_server.py
```

The tests run the server over real HTTP, using bodies laid out like Unity's `WWWForm`. They
do not involve Unity, so the app itself still has to be checked in Play mode.
