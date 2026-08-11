using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Helpers for wiring UI buttons from code.
/// </summary>
public static class ButtonUtils
{
    /// <summary>
    /// Points a button at exactly one handler, replacing whatever was wired from code
    /// before.
    ///
    /// The panels in this project wire their buttons in OnEnable, which runs every time
    /// the panel is reopened - and a plain AddListener stacks on top of the previous open
    /// rather than replacing it. Most of the handlers are idempotent, which is why this
    /// went unnoticed: calling SetActive(true) three times looks the same as calling it
    /// once. The ones that are not idempotent are the ones that matter. Before this,
    /// opening the profile three times and then tapping Confirm-delete once sent three
    /// deletions, and a level change would have moved the user three levels.
    ///
    /// Persistent calls assigned in the Inspector are left alone: RemoveAllListeners only
    /// clears the listeners added at run time.
    /// </summary>
    public static void Rewire(GameObject buttonGO, UnityAction handler)
    {
        Button button = buttonGO.GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(handler);
    }
}
