#nullable enable

using System;

namespace SaySuomi.UserBehaviour
{
    /// <summary>
    /// API for recording ASA learner behaviour (dta-server docs/USER_EVENTS.md).
    ///
    /// Always safe to call: no-op outside a session, and repeated opens or closes are
    /// ignored, so calls can live in OnEnable/OnDisable.
    /// </summary>
    /// <example>
    /// <code>
    /// UserEvents.StartSession();                                  // tap on the ASA button
    /// UserEvents.PanelOpened(new TaskPanelToggled(taskId));       // a panel appears
    /// UserEvents.Track(new MicButtonClicked(MicButton.Record, taskId));
    /// UserEvents.PanelClosed&lt;TaskPanelToggled&gt;();                // it goes away
    /// UserEvents.EndSession();                                    // leaving the assessment
    /// </code>
    /// </example>
    public static class UserEvents
    {
        /// <summary>
        /// Raised when the learner returns after 30 s or more in the background during a session.
        /// The old session has ended and a new one has started on the task list, so the UI
        /// should close any task screen and show the task list.
        /// </summary>
        public static event Action? SessionTimedOut;

        /// <summary>Raises <see cref="SessionTimedOut"/>. Only the manager calls this.</summary>
        internal static void OnSessionTimedOut() => SessionTimedOut?.Invoke();

        /// <summary>
        /// Starts a session unless one is running. Call when the learner taps the Automatic
        /// Speaking Assessment button, and again once onboarding succeeds: a first-time
        /// learner has no guid at the tap, so no session can start then.
        /// </summary>
        public static void StartSession() => UserEventManager.Instance?.StartSession();

        /// <summary>
        /// Closes open panels and ends the session. Call when the learner leaves the
        /// assessment. Closing the app and the background timeout are handled automatically.
        /// </summary>
        public static void EndSession(QuitReason reason = QuitReason.Quit) =>
            UserEventManager.Instance?.EndSession(reason, DateTime.UtcNow);

        /// <summary>Sends the toggle event of the panel with active: true.</summary>
        public static void PanelOpened(IPanelEvent panel) => UserEventManager.Instance?.OpenPanel(panel);

        /// <summary>Sends active: false with the properties the panel opened with.</summary>
        public static void PanelClosed<TPanel>()
            where TPanel : struct, IPanelEvent => UserEventManager.Instance?.ClosePanel(default(TPanel).Name);

        /// <summary>Records a tap or scroll.</summary>
        public static void Track(IInteractionEvent interaction) => UserEventManager.Instance?.Track(interaction);

        /// <summary>Drops the session and queued events unsent. Call on account deletion.</summary>
        public static void Forget() => UserEventManager.Instance?.Forget();
    }
}
