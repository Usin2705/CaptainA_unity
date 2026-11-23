namespace SaySuomi.Modals
{
    /// <summary>
    /// The popup shown while a recorded task is sent and scored. It has its own class so that
    /// ASATaskScreen can ask Navigator for it. The profile's LoadingModal has no class of its own.
    /// </summary>
    public sealed class TaskSendingModal : LoadingModal { }
}
