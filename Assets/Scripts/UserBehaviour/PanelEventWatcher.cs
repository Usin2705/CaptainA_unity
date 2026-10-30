#nullable enable

using System;
using UnityEngine;

namespace SaySuomi.UserBehaviour
{
    /// <summary>
    /// Reports a script-less popup as opened/closed from its OnEnable/OnDisable, so every way
    /// of closing it counts. Added from code, so no scene changes are needed.
    ///
    /// Not for objects that stay active while their content changes (e.g. ASAPanel):
    /// call <see cref="UserEvents.PanelOpened"/> where the content changes instead.
    /// </summary>
    /// <example>
    /// In the owning script, before the popups are shown:
    /// <code>
    /// void Awake()
    /// {
    ///     PanelEventWatcher.Attach(loadingPopUpGO, () => new LoadingModalToggled(CurrentServerTaskId));
    ///     PanelEventWatcher.Attach(
    ///         popupPanelGO,
    ///         () => new DimensionModalToggled(taskId, (ScoreDimension)currentPopupOpen)
    ///     );
    /// }
    /// </code>
    /// </example>
    [DisallowMultipleComponent]
    public sealed class PanelEventWatcher : MonoBehaviour
    {
        Func<IPanelEvent>? describe;
        string? openEventName;

        /// <summary>Watches <paramref name="target"/>. Attach before the popup is first shown.</summary>
        /// <param name="describe">Called on each open, so it reads the current task or dimension.</param>
        public static void Attach(GameObject target, Func<IPanelEvent> describe)
        {
            if (!target.TryGetComponent(out PanelEventWatcher watcher))
            {
                watcher = target.AddComponent<PanelEventWatcher>();
            }

            watcher.describe = describe;
        }

        void OnEnable()
        {
            if (describe is null)
            {
                return;
            }

            IPanelEvent panel = describe();
            openEventName = panel.Name;
            UserEventManager.Instance?.OpenPanel(panel);
        }

        void OnDisable()
        {
            if (openEventName is not null)
            {
                UserEventManager.Instance?.ClosePanel(openEventName);
                openEventName = null;
            }
        }
    }
}
