#nullable enable

using System;
using SaySuomi.Utils;

namespace SaySuomi.UserBehaviour
{
    // One type per server event model (dta-server app/models/events.py). The server silently
    // drops events that do not match, so keep names, keys and bounds in sync.
    // To add an event: copy the closest type and pick the interface for how it is sent.

    /// <summary>An event for POST /analytics/events.</summary>
    /// <example>
    /// Defining an event (server model: <c>task_id: TaskId</c>):
    /// <code>
    /// public readonly struct HintModalToggled : IPanelEvent
    /// {
    ///     public HintModalToggled(int taskId) => TaskId = taskId;
    ///
    ///     public int TaskId { get; }
    ///
    ///     public string Name => "hint_modal_toggled";
    ///
    ///     public string? Validate() => EventRules.ServerId("task_id", TaskId);
    ///
    ///     public void WriteProperties(JsonWriter json) => json.Int("task_id", TaskId);
    /// }
    /// </code>
    /// </example>
    public interface IUserEvent
    {
        /// <summary>Server event name.</summary>
        string Name { get; }

        /// <summary>Why the server would reject the properties, or null if valid.</summary>
        string? Validate();

        /// <summary>Writes the properties the server model declares.</summary>
        void WriteProperties(JsonWriter json);
    }

    /// <summary>A panel or modal. The manager writes <c>active</c>, and implementations write the rest.</summary>
    public interface IPanelEvent : IUserEvent { }

    /// <summary>A one-off tap or scroll.</summary>
    public interface IInteractionEvent : IUserEvent { }

    /// <summary>Mirrors the Button enum on the server.</summary>
    public enum MicButton
    {
        Record,
        Replay,
        Stop,
    }

    /// <summary>
    /// Mirrors the Dimension enum on the server. Ordered like the popups in FeedbackPanel, so a
    /// popup index casts directly.
    /// </summary>
    public enum ScoreDimension
    {
        Proficiency,
        Pronunciation,
        Range,
        Accuracy,
        Fluency,
    }

    /// <summary>Mirrors the QuitReason enum on the server.</summary>
    public enum QuitReason
    {
        Quit,
        Timeout,
        Error,
    }

    /// <summary>Converts the enums above to the strings the server expects.</summary>
    internal static class UserEventWireValues
    {
        /// <summary>The <c>button_id</c> value the server expects.</summary>
        public static string ToWire(this MicButton button) =>
            button switch
            {
                MicButton.Record => "record",
                MicButton.Replay => "replay",
                MicButton.Stop => "stop",
                _ => throw new ArgumentOutOfRangeException(nameof(button), button, null),
            };

        /// <summary>The <c>dimension</c> value the server expects.</summary>
        public static string ToWire(this ScoreDimension dimension) =>
            dimension switch
            {
                ScoreDimension.Proficiency => "proficiency",
                ScoreDimension.Pronunciation => "pronunciation",
                ScoreDimension.Range => "range",
                ScoreDimension.Accuracy => "accuracy",
                ScoreDimension.Fluency => "fluency",
                _ => throw new ArgumentOutOfRangeException(nameof(dimension), dimension, null),
            };

        /// <summary>The <c>reason</c> value the server expects.</summary>
        public static string ToWire(this QuitReason reason) =>
            reason switch
            {
                QuitReason.Quit => "quit",
                QuitReason.Timeout => "timeout",
                QuitReason.Error => "error",
                _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, null),
            };
    }

    /// <summary>Server field constraints. Messages are culture-invariant.</summary>
    internal static class EventRules
    {
        /// <summary>Server ids are >= 1. Catches 0-based task indexes (see ASAPanel.CurrentServerTaskId).</summary>
        public static string? ServerId(string field, int value) =>
            value >= 1 ? null : FormattableString.Invariant($"{field} must be >= 1, was {value}");

        /// <summary>Scroll depth is finite and within [0, 1].</summary>
        public static string? Depth(float value) =>
            float.IsFinite(value) && value >= 0f && value <= 1f
                ? null
                : FormattableString.Invariant($"depth must be within [0, 1], was {value}");
    }

    // ===================== Session =====================
    // Sent by the manager (StartSession / EndSession).

    /// <summary><c>asa_session_started</c>: the first event of a session.</summary>
    public readonly struct AsaSessionStarted : IUserEvent
    {
        public string Name => "asa_session_started";

        /// <summary>No properties to check.</summary>
        public string? Validate() => null;

        /// <summary>No properties to write.</summary>
        public void WriteProperties(JsonWriter json) { }
    }

    /// <summary><c>asa_session_ended</c>: the last event of a session, at the quit time.</summary>
    public readonly struct AsaSessionEnded : IUserEvent
    {
        /// <summary>Creates the event with why the session ended.</summary>
        public AsaSessionEnded(QuitReason reason) => Reason = reason;

        public QuitReason Reason { get; }

        public string Name => "asa_session_ended";

        /// <summary>Nothing to check. The reason is always set from code.</summary>
        public string? Validate() => null;

        /// <summary>Writes <c>reason</c>.</summary>
        public void WriteProperties(JsonWriter json) => json.String("reason", Reason.ToWire());
    }

    // ===================== Panels and modals =====================
    // Sent with UserEvents.PanelOpened / PanelClosed.

    /// <summary><c>task_selection_panel_toggled</c>: the task list.</summary>
    public readonly struct TaskSelectionPanelToggled : IPanelEvent
    {
        public string Name => "task_selection_panel_toggled";

        /// <summary>No properties to check.</summary>
        public string? Validate() => null;

        /// <summary>No properties besides <c>active</c>.</summary>
        public void WriteProperties(JsonWriter json) { }
    }

    /// <summary><c>profile_panel_toggled</c>: the profile of the learner.</summary>
    public readonly struct ProfilePanelToggled : IPanelEvent
    {
        public string Name => "profile_panel_toggled";

        /// <summary>No properties to check.</summary>
        public string? Validate() => null;

        /// <summary>No properties besides <c>active</c>.</summary>
        public void WriteProperties(JsonWriter json) { }
    }

    /// <summary><c>task_panel_toggled</c>: the current task, where the learner records.</summary>
    public readonly struct TaskPanelToggled : IPanelEvent
    {
        /// <summary>Creates the event for the task on screen.</summary>
        public TaskPanelToggled(int taskId) => TaskId = taskId;

        public int TaskId { get; }

        public string Name => "task_panel_toggled";

        /// <summary>Checks <c>task_id</c>.</summary>
        public string? Validate() => EventRules.ServerId("task_id", TaskId);

        /// <summary>Writes <c>task_id</c>.</summary>
        public void WriteProperties(JsonWriter json) => json.Int("task_id", TaskId);
    }

    /// <summary><c>loading_modal_toggled</c>: waiting for the assessment result.</summary>
    public readonly struct LoadingModalToggled : IPanelEvent
    {
        /// <summary>Creates the event for the task being assessed.</summary>
        public LoadingModalToggled(int taskId) => TaskId = taskId;

        public int TaskId { get; }

        public string Name => "loading_modal_toggled";

        /// <summary>Checks <c>task_id</c>.</summary>
        public string? Validate() => EventRules.ServerId("task_id", TaskId);

        /// <summary>Writes <c>task_id</c>.</summary>
        public void WriteProperties(JsonWriter json) => json.Int("task_id", TaskId);
    }

    /// <summary>
    /// <c>feedback_panel_toggled</c>: the assessment results. assessment_id must belong to the
    /// learner, or the server drops the event.
    /// </summary>
    public readonly struct FeedbackPanelToggled : IPanelEvent
    {
        /// <summary>Creates the event for the task and assessment shown.</summary>
        public FeedbackPanelToggled(int taskId, int assessmentId)
        {
            TaskId = taskId;
            AssessmentId = assessmentId;
        }

        public int TaskId { get; }

        public int AssessmentId { get; }

        public string Name => "feedback_panel_toggled";

        /// <summary>Checks <c>task_id</c> and <c>assessment_id</c>.</summary>
        public string? Validate() =>
            EventRules.ServerId("task_id", TaskId) ?? EventRules.ServerId("assessment_id", AssessmentId);

        /// <summary>Writes <c>task_id</c> and <c>assessment_id</c>.</summary>
        public void WriteProperties(JsonWriter json) =>
            json.Int("task_id", TaskId).Int("assessment_id", AssessmentId);
    }

    /// <summary><c>info_modal_toggled</c>: the info popup on the results.</summary>
    public readonly struct InfoModalToggled : IPanelEvent
    {
        /// <summary>Creates the event for the task shown.</summary>
        public InfoModalToggled(int taskId) => TaskId = taskId;

        public int TaskId { get; }

        public string Name => "info_modal_toggled";

        /// <summary>Checks <c>task_id</c>.</summary>
        public string? Validate() => EventRules.ServerId("task_id", TaskId);

        /// <summary>Writes <c>task_id</c>.</summary>
        public void WriteProperties(JsonWriter json) => json.Int("task_id", TaskId);
    }

    /// <summary><c>dimension_modal_toggled</c>: the detail popup for one score dimension.</summary>
    public readonly struct DimensionModalToggled : IPanelEvent
    {
        /// <summary>Creates the event for the task and dimension shown.</summary>
        public DimensionModalToggled(int taskId, ScoreDimension dimension)
        {
            TaskId = taskId;
            Dimension = dimension;
        }

        public int TaskId { get; }

        public ScoreDimension Dimension { get; }

        public string Name => "dimension_modal_toggled";

        /// <summary>
        /// Checks <c>task_id</c> and <c>dimension</c>. The dimension is cast from a popup index,
        /// so it can be out of range.
        /// </summary>
        public string? Validate() =>
            EventRules.ServerId("task_id", TaskId)
            ?? (Enum.IsDefined(typeof(ScoreDimension), Dimension) ? null : $"{Dimension} is not a ScoreDimension value");

        /// <summary>Writes <c>task_id</c> and <c>dimension</c>.</summary>
        public void WriteProperties(JsonWriter json) =>
            json.Int("task_id", TaskId).String("dimension", Dimension.ToWire());
    }

    // ===================== Interactions =====================
    // Sent with UserEvents.Track.

    /// <summary><c>mic_button_clicked</c>: record, stop or replay on the task panel.</summary>
    public readonly struct MicButtonClicked : IInteractionEvent
    {
        /// <summary>Creates the event for the button tapped on a task.</summary>
        public MicButtonClicked(MicButton button, int taskId)
        {
            Button = button;
            TaskId = taskId;
        }

        public MicButton Button { get; }

        public int TaskId { get; }

        public string Name => "mic_button_clicked";

        /// <summary>Checks <c>task_id</c>. The button is always set from code.</summary>
        public string? Validate() => EventRules.ServerId("task_id", TaskId);

        /// <summary>Writes <c>button_id</c> and <c>task_id</c>.</summary>
        public void WriteProperties(JsonWriter json) =>
            json.String("button_id", Button.ToWire()).Int("task_id", TaskId);
    }

    /// <summary><c>profile_panel_scrolled</c>: depth 0 = top, 1 = bottom.</summary>
    public readonly struct ProfilePanelScrolled : IInteractionEvent
    {
        /// <summary>Creates the event for a scroll at rest.</summary>
        public ProfilePanelScrolled(float depth) => Depth = depth;

        public float Depth { get; }

        public string Name => "profile_panel_scrolled";

        /// <summary>Checks <c>depth</c>.</summary>
        public string? Validate() => EventRules.Depth(Depth);

        /// <summary>Writes <c>depth</c>, rounded to 3 decimals. Finer is noise.</summary>
        public void WriteProperties(JsonWriter json) => json.Number("depth", Math.Round(Depth, 3));
    }

    /// <summary><c>feedback_panel_scrolled</c>: depth 0 = top, 1 = bottom.</summary>
    public readonly struct FeedbackPanelScrolled : IInteractionEvent
    {
        /// <summary>Creates the event for a scroll at rest on the results of a task.</summary>
        public FeedbackPanelScrolled(float depth, int taskId)
        {
            Depth = depth;
            TaskId = taskId;
        }

        public float Depth { get; }

        public int TaskId { get; }

        public string Name => "feedback_panel_scrolled";

        /// <summary>Checks <c>depth</c> and <c>task_id</c>.</summary>
        public string? Validate() => EventRules.Depth(Depth) ?? EventRules.ServerId("task_id", TaskId);

        /// <summary>Writes <c>depth</c> (3 decimals) and <c>task_id</c>.</summary>
        public void WriteProperties(JsonWriter json) =>
            json.Number("depth", Math.Round(Depth, 3)).Int("task_id", TaskId);
    }
}
