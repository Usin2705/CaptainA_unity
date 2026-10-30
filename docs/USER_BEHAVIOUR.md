# User Behaviour Events (client)

How the Unity client records what learners do in the Automatic Speaking Assessment (ASA)
and sends it to the server. Server contract: dta-server `docs/USER_EVENTS.md` and
`app/models/events.py`.

Status: first draft. The system is in `Assets/Scripts/UserBehaviour/` but is not yet
called from the ASA screens (see [Wiring](#wiring)).

## How it works

1. **Session.** Events are only recorded inside an ASA session. It starts when the learner
   taps **Automatic Speaking Assessment** (`StartSession()` sends `asa_session_started`
   with a new `asa_session_id`) and ends only when they quit: leaving the assessment
   (`EndSession()`), closing the app, or 30 s or more in the background (after which a new
   session starts on the task list). Ending closes any open
   panels, then sends `asa_session_ended`. Outside a session every call does nothing.
2. **Record.** Each event is checked against the server's rules (ids ≥ 1, depth 0–1, and
   a dimension in range, since it is cast from a popup index). Invalid ones are logged
   and skipped. Valid ones get an
   `event_id` and a UTC timestamp and are written to `user_events_outbox.json` straight away.
3. **Send.** Every 60 s, at 20 queued events, and when a session ends, queued events are
   posted to `Secret.ASA_EVENTS_URL` in batches of ≤ 100, one session per batch.
4. **Response.**

   | Status                     | Action                                       |
   | -------------------------- | -------------------------------------------- |
   | 2xx                        | Remove batch.                                |
   | 400/413/415/422, other 4xx | Remove batch, log error (client bug).        |
   | 403/404/409                | Remove batch, log warning (no consent/user). |
   | 5xx, 429, offline          | Keep batch; the next 60 s tick retries.      |

   A resent event keeps its `event_id`, so the server stores it once.

## Panels

A panel event says _which_ panel and _what it shows_; the manager adds `active`.

- `PanelOpened(p)` sends `active: true`; `PanelClosed<T>()` sends `active: false` with the
  same `task_id`/`dimension`, so the server can pair them into durations.
- Opening a panel that is already open is ignored; opening it with new content (another
  task) closes the old one first. Closing one that is not open is ignored. So calls can
  live in `OnEnable`/`OnDisable`.
- `PanelEventWatcher.Attach(go, () => new LoadingModalToggled(id))` reports a script-less
  popup from its own enable/disable.
- `ScrollDepthReporter.Attach(scrollRect, depth => ...)` reports scroll depth once a
  learner's scroll comes to rest (0 = top, 1 = bottom). Programmatic resets don't count.

## Events

| Event                          | Properties                           | Call                                              |
| ------------------------------ | ------------------------------------ | ------------------------------------------------- |
| `asa_session_started`          | –                                    | `UserEvents.StartSession()`                       |
| `asa_session_ended`            | `reason`: quit/timeout/error         | `UserEvents.EndSession(reason)`                   |
| `task_selection_panel_toggled` | `active`                             | `PanelOpened(new TaskSelectionPanelToggled())`    |
| `profile_panel_toggled`        | `active`                             | `PanelOpened(new ProfilePanelToggled())`          |
| `task_panel_toggled`           | `active`, `task_id`                  | `PanelOpened(new TaskPanelToggled(id))`           |
| `loading_modal_toggled`        | `active`, `task_id`                  | `PanelOpened(new LoadingModalToggled(id))`        |
| `feedback_panel_toggled`       | `active`, `task_id`, `assessment_id` | `PanelOpened(new FeedbackPanelToggled(id, aid))`  |
| `info_modal_toggled`           | `active`, `task_id`                  | `PanelOpened(new InfoModalToggled(id))`           |
| `dimension_modal_toggled`      | `active`, `task_id`, `dimension`     | `PanelOpened(new DimensionModalToggled(id, dim))` |
| `mic_button_clicked`           | `button_id`, `task_id`               | `Track(new MicButtonClicked(button, id))`         |
| `profile_panel_scrolled`       | `depth`                              | `Track(new ProfilePanelScrolled(depth))`          |
| `feedback_panel_scrolled`      | `depth`, `task_id`                   | `Track(new FeedbackPanelScrolled(depth, id))`     |

`task_id` is the server id (1-based): use `ASAPanel.CurrentServerTaskId`, not the array index.

To add an event: copy the closest struct in `UserEventModels.cs`, change `Name`, fields,
`Validate` and `WriteProperties`, and keep it in step with `events.py`.

## Lifecycle on a phone

| Situation                        | What happens                                                                                                                                                               |
| -------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| App goes to background           | Time it was left saved; queued events sent.                                                                                                                                |
| Back in under 30 s               | Same session continues.                                                                                                                                                    |
| Back after 30 s or more          | Session ends as `timeout` at the time it was left. A new session (new `asa_session_id`) starts on the task list, and `UserEvents.SessionTimedOut` tells the UI to show it. |
| App killed in background / crash | Next launch ends that session as `timeout` at last-seen time.                                                                                                              |
| App quits normally               | Session ends as `quit`.                                                                                                                                                    |
| Account deleted                  | `UserEvents.Forget()` drops the session and queue unsent.                                                                                                                  |
| Offline                          | Events stay queued; max 200 on device, oldest dropped first.                                                                                                               |

## Files

| File                                 | Role                                                           |
| ------------------------------------ | -------------------------------------------------------------- |
| `UserEvents.cs`                      | Public API the screens call.                                   |
| `UserEventManager.cs`                | Session, open panels, recording, send loop. Created at launch. |
| `UserEventModels.cs`                 | One struct per server event, plus enums and rules.             |
| `UserEventOutbox.cs`                 | On-device queue (JSON file, max 200).                          |
| `UserEventSender.cs`                 | One HTTP POST; maps status codes to outcomes.                  |
| `Assets/Scripts/Utils/JsonWriter.cs` | Minimal JSON writer for the payload (`SaySuomi.Utils`).        |
| `PanelEventWatcher.cs`               | Reports popup open/close from enable/disable.                  |
| `ScrollDepthReporter.cs`             | Reports scroll depth at rest.                                  |

## Wiring

Not done yet. Where the calls belong:

- **Start:** `AdvancePanel.OnASAButtonClicked` → `StartSession()`. Also after onboarding
  succeeds in `AdvancePanel.GetUserInput`: a first-time learner has no guid at the tap.
- **End:** leaving the assessment → `EndSession()`: the TaskPanel back button and
  `AdvancePanel.OnDisable` (another tab). Closing the app is automatic.
  `NetworkManager.ClearLocalASAData` → `Forget()`.
- **Task list / profile:** open in `OnEnable`, close in `OnDisable`.
- **Task panel:** open in `ASAPanel.DisplayTask`; close in `ReturnToTaskPanel`, the
  feedback panel's "new task" handler, and `OnDisable`. Not `OnEnable`: `ASAPanel` stays
  active under the task list, so it does not re-enable for the next task.
- **Mic:** `Track(...)` in the record/stop/replay click handlers in `ASAPanel.OnEnable`.
  Only taps count, so not in `StopRecord`: the timer running out and the back button also
  call it.
- **Popups:** `PanelEventWatcher` on the loading, info and dimension popups; ids from
  `asrResultASA` in the feedback panel.
- **Scroll:** `ScrollDepthReporter` on the feedback and profile scroll views.
- **Timeout:** subscribe to `UserEvents.SessionTimedOut` (e.g. in `AdvancePanel.OnEnable`,
  unsubscribe in `OnDisable`). The handler switches off the current task (`ASAPanelGO`),
  results (`feedbackPanelGO`), the loading popup, the profile and the dim panel, then
  switches on the task list (`taskPanelGO`). `ASAPanel.OnDisable` already cancels a
  recording in progress. Check in Play mode that no popup outside these stays on screen.

## Open questions

- What should trigger `reason: error`? Nothing does yet.
