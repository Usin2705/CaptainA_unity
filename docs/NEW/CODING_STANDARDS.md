# Coding Standards

Conventions for the scripts and documentation in this project. For how screens connect to their user
interface, see [Wiring](WIRING.md).

## Formatting

Run after every change, from the folder that contains the changed files:

```sh
csharpier format .                # C#
npx prettier --write "**/*.md"    # Markdown
ruff format .                     # Python
```

## Namespaces

The root namespace is `SaySuomi`. Each script belongs to it or to a namespace below it, named after
the relevant area of the application:

| Folder                    | Namespace                          | Contains                                      |
| ------------------------- | ---------------------------------- | --------------------------------------------- |
| `Navigation`              | `SaySuomi.Navigation`              | `Navigator`, the base screens and the tab bar |
| `Navigation/Experimental` | `SaySuomi.Navigation.Experimental` | Trial replacements for navigation classes     |
| `Screens`                 | `SaySuomi.Screens`                 | Screens and the classes that support them     |
| `Modals`                  | `SaySuomi.Modals`                  | Modals                                        |
| `Managers`                | `SaySuomi.Managers`                | Audio, the server and their data classes      |
| `Editor`                  | `SaySuomi.EditorTools`             | Tools that run only in the Unity Editor       |

`SaySuomi.Editor` is avoided because that namespace would hide
[`UnityEditor.Editor`](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Editor.html).

```csharp
using SaySuomi.Navigation;

namespace SaySuomi.Screens
{
    public class ExampleScreen : AppScreen { }
}
```

Unity adds `SaySuomi` to new scripts through **Project Settings > Editor**. The remainder, such as
`.Screens`, is added by hand.

Unity stores the full type name of each Inspector `OnClick` target in the scene, so moving a class to
another namespace also means updating its `m_TargetAssemblyTypeName` entries.

## Naming

| What                    | Rule              | Example                              |
| ----------------------- | ----------------- | ------------------------------------ |
| Full-screen view        | Ends in `Screen`  | `FlashcardsScreen`                   |
| Popup                   | Ends in `Modal`   | `SurveyModal`                        |
| Section inside a screen | May use "Panel"   | `ResultPanel`                        |
| Abbreviation            | Stays in capitals | `ASATaskScreen`, not `AsaTaskScreen` |

A class must not be named `Screen`, because that name would hide
[`UnityEngine.Screen`](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Screen.html).

Unity stores serialized fields by name, so a rename without
[`[FormerlySerializedAs]`](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Serialization.FormerlySerializedAsAttribute.html)
silently clears the assigned value:

```csharp
// The old field name, so the object assigned in the Inspector is kept.
[FormerlySerializedAs("sendButtonGO")]
[SerializeField]
private Button sendButton;
```

## Member order

Members follow this order, and each states its access level explicitly, including `private`.

| #   | Group                                                                                                                             | Example                                   | Order within the group                             |
| --- | --------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------- | -------------------------------------------------- |
| 1   | Constants                                                                                                                         | `private const float FadeSeconds = 0.2f;` | `public` to `private`                              |
| 2   | Static fields                                                                                                                     | `private static int openCount;`           | As written, because initializers run top to bottom |
| 3   | Serialized fields, marked [`[SerializeField]`](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/SerializeField.html) | `private Button openButton;`              | Inspector order                                    |
| 4   | Other fields                                                                                                                      | `private bool isWaiting;`                 | `public` to `private`                              |
| 5   | Constructors                                                                                                                      | `public Example() { }`                    | `public` to `private`                              |
| 6   | Events and delegates                                                                                                              | `public event Action TabChanged;`         | `public` to `private`, `static` first              |
| 7   | Properties                                                                                                                        | `public AppScreen Root => root;`          | `public` to `private`, `static` first              |
| 8   | [Unity messages](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/MonoBehaviour.html) and `Bind`                     | `private void OnEnable() { }`             | Execution order, listed below                      |
| 9   | Other methods                                                                                                                     | `public void Open() { }`                  | `public` to `private`, `static` first              |
| 10  | Nested types                                                                                                                      | `public class TaskAttributes { }`         | `public` to `private`                              |

`Bind` is the setup method defined by this project. The
[execution order](https://docs.unity3d.com/6000.5/Documentation/Manual/execution-order.html) for group 8 is `Awake`, `Bind`, `OnEnable`, `Start`,
`Update`, `LateUpdate`, `OnDisable`, `OnDestroy`.

The numbered comments below only label the groups, and should not be copied into production code.

```csharp
public class ExampleScreen : AppScreen
{
    // 1. Constants
    private const float FadeSeconds = 0.2f;

    // 2. Static fields
    private static int openCount;

    // 3. Serialized fields
    [SerializeField]
    private Button openButton;

    // 4. Other fields
    private bool isWaiting;

    // 7. Properties
    public override ScreenLayer Layer => ScreenLayer.Overlay;

    // 8. Unity messages and Bind. Bind connects the buttons, because it only runs once.
    protected override void Bind()
    {
        ButtonWiring.Replace(openButton, Open);
    }

    // OnEnable runs every time the screen opens.
    private void OnEnable()
    {
        isWaiting = false;
    }

    // 9. Other methods
    public void Open()
    {
        openCount++;
    }
}
```

## Inspector

- **[`[Header]`](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/HeaderAttribute.html):**
  groups fields by hierarchy location, beginning with objects on the screen itself. Uses title case.
  May be omitted from small classes.
- **[`[Tooltip]`](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/TooltipAttribute.html):**
  provides what the name cannot convey, such as visibility conditions, array order or required
  components. Uses sentence case.
- **[`[System.NonSerialized]`](https://learn.microsoft.com/dotnet/api/system.nonserializedattribute):**
  keeps a public, runtime-only field out of the Inspector.

```csharp
// The header starts a group, and the tooltip appears when hovering over the field.
[Header("Consent Modal")]
[Tooltip("The accept button stays disabled until this is ticked.")]
[SerializeField]
private Toggle consentReadToggle;

// Public, but only used while the app runs, so it stays out of the Inspector.
[System.NonSerialized]
public int currentTaskSelected = -1;
```

## Comments

- **Appropriate uses:** the reason for a non-obvious decision, unexpected behaviour, the source of a
  rule, and `///` on public APIs.
- **Inappropriate uses:** restating the code, compensating for a poor name, recording history, and
  commented-out code.
- **Style:** short and plain, in the present tense, without contractions. Comments are updated or
  removed when the code changes.

```csharp
// The server task IDs start at 1, but this index starts at 0.
int taskId = index + 1;
```

## Further reading

**Project documents**

- [Navigation](NAVIGATION.md)
- [Screens](SCREENS.md)
- [Transitions](TRANSITIONS.md)
- [Wiring](WIRING.md)
- [Migration](MIGRATION.md)

**External references**

- [Unity Scripting API](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/index.html)
- [Unity Manual](https://docs.unity3d.com/6000.5/Documentation/Manual/index.html)
- [C# coding conventions](https://learn.microsoft.com/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [CSharpier](https://csharpier.com)
- [Prettier](https://prettier.io)
- [Ruff](https://docs.astral.sh/ruff/formatter/)
- [Mermaid](https://mermaid.js.org)
