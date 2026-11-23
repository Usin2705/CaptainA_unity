# Screen Navigation

How the app moves between screens, overlays and modals.

Related: [Screens](SCREENS.md), [Transitions](TRANSITIONS.md), [Wiring](WIRING.md),
[Migration](MIGRATION.md), [Coding Standards](CODING_STANDARDS.md).

## Overview

One class, `Navigator`, owns all navigation. Screens never switch each other on or off:
they ask `Navigator` to open a screen by type, and it handles history, back, visibility
and animation. Code lives under `Assets/Scripts/NewScripts/` in the `Navigation`, `Screens`,
`Modals` and `Managers` folders, each with [its own namespace](CODING_STANDARDS.md#namespaces). The
classes below are in `SaySuomi.Navigation`, apart from `LoadingModal`, which is in `SaySuomi.Modals`.

| Class               | Role                                                                            |
| ------------------- | ------------------------------------------------------------------------------- |
| `Navigator`         | Owns the stack, the current tab and back handling. Created at launch.           |
| `AppScreen`         | Base class for every screen. Declares its layer and back behaviour.             |
| `ModalScreen`       | `AppScreen` for popups. Adds `Attach`, `Dismissible`, `OnBack` and `OnDiscard`. |
| `LoadingModal`      | `ModalScreen` for popups waiting on the server. Spins `LoadingIcon`.            |
| `ModalHost`         | Presents modals: the shared dim and the modal animations.                       |
| `ScreenTransitions` | The animation curves and durations, and the runner that plays them.             |
| `TabBar`            | Tab buttons, icons and the new-feature dot. Follows `TabChanged`.               |
| `ErrorBanner`       | Shows and hides the drop-down error messages.                                   |
| `ButtonWiring`      | Connects buttons in code and switches off their Inspector entries.              |
| `ScreenBackButton`  | Adds a back button to screens that have none. See [Back](#back).                |

## Layers

Each screen declares its layer in code by overriding `Layer`. Draw order comes from sibling
order under the root Canvas, so a higher layer always covers a lower one.

| Layer     | Examples                     | The screen below                                  | Covers the tab bar |
| --------- | ---------------------------- | ------------------------------------------------- | ------------------ |
| `Screen`  | Task list, flashcard session | Hidden, and refreshed by its `OnEnable` on return | No                 |
| `Overlay` | Score detail, phoneme, info  | Stays active, so its state stays in place         | Yes                |
| Modal     | Survey, consent, loading     | Stays active, behind the shared dim               | Yes                |

Screens are hidden with `SetActive`, not by disabling their Canvas. They rely on
`OnEnable` and `OnDisable` to load data, cancel recordings and remove listeners.

## The stack

The current tab root is always at the bottom. Everything opened from it sits above, and
the top entry receives back presses.

```mermaid
flowchart BT
    root["Tab root: FlashcardsScreen"]
    session["Screen: FlashcardSessionScreen"]
    detail["Overlay: ScoreDetailScreen"]
    phoneme["Overlay: PhonemeScreen (top, receives back)"]
    root --> session --> detail --> phoneme
```

In this example, `FlashcardsScreen` is hidden because a `Screen` layer entry covers it.
`FlashcardSessionScreen` and `ScoreDetailScreen` stay active under their overlays.

## API

```csharp
public event Action<AppScreen> TabChanged;

// Returns the screen so the caller can give it data.
public T Push<T>()
    where T : AppScreen;

public T Open<T>()
    where T : ModalScreen;

// For modals with no class of their own.
public void Open(ModalScreen modal);

// The only Escape handler in the app.
public void Back();

public void PopTo<T>()
    where T : AppScreen;

public void PopToRoot();

// Clears the stack. Does nothing when already on the root of that tab.
public void SelectTab(AppScreen tabRoot);

public bool IsOpen(AppScreen screen);

public T Get<T>()
    where T : AppScreen;
```

Data passes through typed methods on the returned screen, so there is no generic
arguments object:

```csharp
Navigator.GetManager().Push<ASATaskScreen>().DisplayTask(task);
```

The screen is active by the time `Push` returns, so its `OnEnable` has already run when
`DisplayTask` is called.

## Opening a screen

```mermaid
flowchart TD
    start["Push is called"] --> running{"Transition running?"}
    running -- Yes --> snap["Snap it to its end state"]
    running -- No --> activate
    snap --> activate["Add the new screen to the stack and activate it"]
    activate --> returned["Return the screen to the caller"]
    returned --> wait["Wait one frame"]
    wait --> animate["Animate, with input blocked on both screens"]
    animate --> layer{"Is the new screen on the Screen layer?"}
    layer -- Yes --> hide["Hide everything below it"]
    layer -- No --> keep["Keep the screen below active"]
    hide --> finish["Restore position, scale and alpha, then allow input"]
    keep --> finish
```

## Back

Only `Navigator` reads Escape, which is how Unity reports the Android back button. One
press therefore acts on exactly one layer.

```mermaid
flowchart TD
    press["Back pressed"] --> blocked{"Is the top entry a non-dismissible modal?"}
    blocked -- Yes --> ignored["Stop: ignore the press"]
    blocked -- No --> handled{"Does the top entry's HandleBack return true?"}
    handled -- Yes --> consumed["Stop: the screen handled it"]
    handled -- No --> root{"Is the top entry a tab root?"}
    root -- No --> pop["Pop the top entry"]
    root -- Yes --> freestyle{"Is the tab Freestyle?"}
    freestyle -- No --> select["Select the Freestyle tab"]
    freestyle -- Yes --> quit["Application.Quit"]
```

Override `HandleBack` only when a screen must do something other than close. For example,
`ASATaskScreen` stops a running recording and returns true, so the first press stops the
recording and the next one leaves. `ASAResultsScreen` returns to the task list rather than the
task it covers.

`Application.Quit` does nothing in the Editor, so exiting has to be tested on a device.

iPhones have no back key, so every screen needs a visible way back. A screen with no back
control of its own overrides `ShowsBackButton` to return true. At launch, `Navigator` adds a copy
of the task screen's back button to the top-left of its header, which calls `Back`. The flashcard
session and NumberGame use this.

## Tabs

`SelectTab` clears the stack, then shows the chosen tab root. Returning to a tab therefore
always lands on its root. Selecting the tab whose root is already showing does nothing.

`TabBar` updates from `TabChanged` rather than from its own button clicks, so its icons
stay correct when back selects Freestyle.

## Modals

`Navigator` keeps modals on the same stack as everything else, so back order stays in one
place. `ModalHost`, which `Navigator` adds to its own object, only handles how they are
presented:

- It puts the newest modal on top, then keeps the scene's one `ModalDim` directly below that
  modal. The dim blocks input to everything beneath it, including the tab bar.
- It runs the modal open and close animations.

Every popup lives under `Canvas/Modals`, which draws after the tab bar and everything else. A
screen that opens one holds a reference to it, so a popup can be used from any screen.

A tap on the dim counts as back, so it follows the same rules: a non-dismissible modal ignores it,
`OnBack` can keep the modal open, and otherwise the modal closes. The tap is ignored while a modal
animates, because the dim stays up during the close animation, and a second tap would otherwise go
back on the screen below, which quits the app on the home tab. A modal's panel needs a Raycast Target
`Image`, or taps on it pass through to the dim and close the modal.

`ModalScreen.OnDiscard` clears a popup's form when back closes it, and records a decline where the
popup counts one. A tap on the dim skips it while
any input field in the popup has text, so what was typed is still there when the popup next opens.
The popup's own back button and the back key still discard. `ASAResultsScreen` also clears its form
when it opens, because a comment kept from one result must not be sent with the next.

`ModalScreen.OnBack` runs whenever back closes a popup, including from its own back button if
that calls `Navigator.Back`. It is the place for what must happen on every close, such as hiding an
error message. Return true from it to keep the popup open.

Drop-down error messages go through `ErrorBanner`, which moves them to the top of the Canvas
so they draw above the popup they belong to.

`Dismissible` is false while a loading popup is waiting on the server, then true once it
shows an error, so back can leave a failed request but not one in progress.

## Startup and scenes

`Navigator` creates itself with `[RuntimeInitializeOnLoadMethod]`, as `UserEventManager`
does, and finds every `AppScreen` in the scene, including inactive ones. It calls `Bind` on each
one, then hides the other tab roots and shows Freestyle with no animation.

Because of this, an unassigned field appears in the Console in every Play session, even for a
screen that is never opened. If a screen's `Bind` fails, the rest are still set up and all failures are
logged together, and the app still opens.

Leave tab roots other than Freestyle inactive in the scene. `Navigator` runs after scene
objects are enabled, so an active root's `OnEnable` would already have run. For Advanced,
that clears the new-feature dot at launch.

The old `PopUpManager` must not be in the scene, because its `Start` would hide the instruction
popup while it is open.

That launch hook runs in every scene. If a scene has no `AppScreen`, `Navigator` removes
itself and does nothing, so scenes using the old panels are unaffected.

## Opening popups from `OnEnable`

A screen's `OnEnable` can open a modal, as `PhonemeScreen` does for its first-time
instruction. `Push` and `Open` add an entry to the stack before activating it, so the modal
lands above the screen that opened it.

`InstructionModal.Show` does nothing if `Navigator` does not exist yet, which happens when a
screen is active in the scene. `Navigator` then re-enables the home tab once it is ready, so
that `OnEnable` runs again and the popup opens.

## Network responses

`NetworkManager` contains no UI: no Inspector fields, UI types or references to screens. It
sends requests, stores results, and reports back through callbacks. The requesting screen
then updates its own UI and decides where to go.

| Request                 | Callback receives        | On failure, read             | Handled by                             |
| ----------------------- | ------------------------ | ---------------------------- | -------------------------------------- |
| `ServerPost`            | `bool`                   | `lastError`                  | Freestyle, flashcard session           |
| `NumberGamePost`        | `bool`                   | `lastError`                  | `NumberGameScreen`                     |
| `ServerPost_ASA`        | `bool`                   | `lastServerError`            | `ASATaskScreen`                        |
| `ServerPost_profile`    | Response JSON, or `null` | `lastServerError`            | `ASATaskListScreen`                    |
| `ServerPost_setLevel`   | `bool`                   | `lastServerError`            | `ASAProfileScreen`                     |
| `ServerPost_guid`       | `bool`                   | `lastError`, `lastErrorType` | `AdvancedScreen`                       |
| `ServerPost_deleteUser` | `bool`                   | `DataConfirmedDeleted`       | `ASAProfileScreen`                     |
| `ServerPost_feedback`   | `bool`, optional         | `lastError`                  | Task list, profile, `FeedbackAutoSend` |

- **Results:** read `asrResult` or `asrResultASA` from the callback. `ASAProfileScreen.ShowStats`
  parses profile JSON, so the data types stay with the screen.
- **Localized errors:** `ServerNotices.Show(label, lastServerError)` picks the message for a
  `ServerError`, using table entries defined in code.
- **Surveys:** `NetworkManager.SurveyDue` fires when a recording reaches the survey threshold,
  and `Navigator` opens `SurveyModal`.
- **Level changes:** check `CanSetLevel` before showing progress. A build with a blank
  `Secret.cs` has no endpoint, so nothing is sent.

A coroutine runs on whichever object started it and stops if that object is disabled. A
request that must outlive a screen runs on `NetworkManager` instead: `FeedbackAutoSend`, because
the task screen hides behind Results, and the profile reload, because the task list is hidden
behind the profile.
