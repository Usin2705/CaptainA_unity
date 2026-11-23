# UI Wiring

How screens connect to their UI, and how to add a new one. Scene objects are assigned in the
Inspector, and behaviour such as button clicks is wired in code.

For naming, member order and Inspector layout, see [Coding Standards](CODING_STANDARDS.md).

## Rules

| Need                        | Do this                                                                                                        |
| --------------------------- | -------------------------------------------------------------------------------------------------------------- |
| Child object or component   | A `[SerializeField]` field, assigned in the Inspector                                                          |
| Button click                | `ButtonWiring.Replace(button, Handler)` once, in `Bind`                                                        |
| Back control                | Your own button calling `Navigator.Back`, or override `ShowsBackButton` to get the shared one                  |
| Localized text              | `new LocalizedString(table, key)`                                                                              |
| Another screen              | Ask `Navigator` by type. Never hold a reference to a screen.                                                   |
| Screen layer                | Override `Layer`                                                                                               |
| Object outside the screen   | Give it a class, and ask `Navigator` for it or look for it with `GetComponentInChildren`                       |
| Value carried by an object  | A component holding the value, such as `OptionValue` or `FlashcardDeck`. Never read it from the object's name. |
| Popup with no logic         | `ModalScreen.Attach(popupGO, dismissible)` from the owning screen                                              |
| Popup waiting on the server | A `LoadingModal` component on the popup, with its `icon` assigned, which it spins                              |
| Reset on closing a popup    | `modal.OnDiscard`, which a tap on the dim skips while text is typed                                            |
| Drop-down error message     | `ErrorBanner.Show` and `ErrorBanner.Hide`                                                                      |
| Durations and constants     | Constants in code                                                                                              |

A serialized reference keeps working when its object is renamed or moved.

**Wire buttons in `Bind`, not `OnEnable`.** `OnEnable` runs every time a screen reopens, and a
repeated `AddListener` makes one tap run its handler several times.

`Navigator` calls `Bind` once for every screen at launch, while most are inactive. Only connect
events there. Anything that depends on the screen being open belongs in `OnEnable`.

`ButtonWiring.Replace` also switches off any Inspector `OnClick` entries while the app runs,
so a migrated button cannot fire twice and its entries do not have to be cleared by hand.

## What stays in the Inspector

- Script components on screen roots and on the tab bar
- Prefab references, such as `ListItemPanel` and `ListDetailPanel`
- Every screen's and modal's references to its objects
- `ASATaskScreen.tasks`, because it holds sprites
- `AudioManager.recordNotif`, because it is an audio clip
- `TabButton`: its screen and its two icons
- `FlashcardDeck`: its deck file name, whether it is the secret deck, and its text and
  progress bar
- `OptionValue`: the value a toggle sends to the server
- `ModalDim`: its colour and transparency, on its `Image`, which also needs Raycast Target on, so that
  it blocks taps below and a tap on it goes back
- Layout and visual values on UI components

`NetworkManager` needs nothing assigned, because it contains no UI.

## Renaming and moving

| Change                                 | Effect                                                     |
| -------------------------------------- | ---------------------------------------------------------- |
| Rename or move anything in the scene   | Safe: references and classes do not depend on names        |
| Delete an object a field points to     | Logged at launch as an unassigned field                    |
| Rename a child inside a runtime prefab | Breaks its `transform.Find`, because those are still paths |

`MissingReferences` logs an error naming the class and the field for any that is empty. It runs
for every screen at launch, so the error appears in every Play session, even if that screen is
never opened.

## Hierarchy

Sibling order sets draw order, so this order keeps tabs below overlays and modals:

```text
Canvas
├── Tabs       FreestyleScreen, FlashcardsScreen, AdvancedScreen, ProgressScreen
├── Screens    FlashcardSessionScreen, NumberGameScreen, ASATaskListScreen, ASATaskScreen,
│              ASAResultsScreen, ASAProfileScreen
├── TabBar     Freestyle, Flashcard, Advanced (holds NewFeatureBadgeGO), Progress
├── Overlays   ScoreDetailScreen, PhonemeScreen, InfoScreen
└── Modals     ModalDim, then every popup
```

`Tabs`, `Screens`, `Overlays` and `Modals` are plain groups that only hold other objects. Each
must stay active, and its `RectTransform` must stretch to fill the Canvas, or the screens inside
it are hidden or sized wrongly.

Each screen root and app-wide modal is named after its class. `Canvas` must stay the scene root,
because `ErrorBanner` relies on `transform.root`.

`Navigator`, `ModalHost` and each screen's nested Canvas are all created
from code, so they do not appear in the scene.

Every popup goes directly under `Modals`, never under a screen or in a group inside `Modals`.
`ModalHost` only orders a popup among its siblings, so one in a group could draw over a newer
popup. Popups used by one screen start with that screen's name, such as `ASAProfileSettingsModal`.

## Finding objects

| Object                                  | Found by                                                              |
| --------------------------------------- | --------------------------------------------------------------------- |
| Screens and app-wide modals             | Class, through `Navigator`                                            |
| Tab buttons                             | `TabButton`, in hierarchy order, so adding a tab needs no code change |
| New-feature dot                         | `NewFeatureDot`                                                       |
| Objects inside a screen or modal        | `[SerializeField]` fields, checked at launch                          |
| Children of a prefab created at runtime | `transform.Find` on the new instance, because it is not in the scene  |

## Experimental tab button

`SaySuomi.Navigation.Experimental.TabButton` is a trial replacement for `TabButton`. It takes a
background icon, a label and an icon for the label, instead of an active and an inactive icon. The
label is a `LocalizedString` or a plain string, and the localized one wins when both are set. It takes its colours from the `Button` on the same object, and tints the background, the label icon
and the label with them: **Normal Color**, **Selected Color** for the active tab, and **Pressed
Color** while the tab is pressed or held. Pressed wins, and sliding off the tab while holding it shows
the colour underneath, because releasing there does not click. Only the Freestyle tab uses it so far.

It follows `TabChanged` itself, because `TabBar` only finds the original `TabButton`, so it needs no
change to `TabBar`. The new-feature dot stays with `TabBar`.

To try it on a tab:

1. Remove the original `TabButton` from the tab's button. Keep its `Button` and `Image`, because that
   `Image` becomes the background.
2. Add a child `Image` for the label icon and a child `TextMeshProUGUI` for the label.
3. Add the experimental `TabButton` and assign its screen, background icon, label icon, one of the
   two labels and the two children.
4. Set the colours on the `Button`. Leave its **Transition** on **Color Tint** so they stay visible in
   the Inspector. `TabButton` turns the transition off when the app runs and applies the tint itself,
   because the transition only tints one graphic and has no state for the active tab.

## Adding a screen

1. Create the GameObject under `Screens` for a `Screen` layer entry, or under `Overlays` for an
   overlay, and leave it inactive. Name it after its class.
2. Add a class and attach it to the root:

   ```csharp
   using SaySuomi.Navigation;
   using TMPro;
   using UnityEngine;
   using UnityEngine.UI;

   namespace SaySuomi.Screens
   {
       public class ExampleScreen : AppScreen
       {
           [SerializeField]
           private TextMeshProUGUI titleText;

           [SerializeField]
           private Button backButton;

           // Omit this for the default Screen layer.
           public override ScreenLayer Layer => ScreenLayer.Overlay;

           protected override void Bind()
           {
               ButtonWiring.Replace(backButton, () => Navigator.GetManager().Back());
           }

           public void Show(string title)
           {
               titleText.text = title;
           }
       }
   }
   ```

3. Assign its fields in the Inspector.

4. Open it from the caller:

   ```csharp
   Navigator.GetManager().Push<ExampleScreen>().Show("Hello");
   ```

5. Add it to [Screens](SCREENS.md).

There is no Escape handling or registration to add. `Navigator` finds the screen and
handles back.

## Stopping work when a screen closes

Put cleanup in `OnDisable`, which runs on every way of leaving a screen, including tab
changes. Any screen that records audio must cancel the recording there. If a back press
should do something other than close the screen, override `HandleBack` and return true.
