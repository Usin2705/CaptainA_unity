#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using SaySuomi.Utils;
using UnityEngine;

namespace SaySuomi.UserBehaviour
{
    /// <summary>
    /// Records <see cref="UserEvents"/> to an on-device outbox and sends it in batches.
    /// Events only count inside an ASA session. Created automatically at launch.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UserEventManager : MonoBehaviour
    {
        const int MaxBatchSize = 100; // Server limit.
        const int SendAtQueueSize = 20;
        const float SendEverySeconds = 60f;
        const string OutboxFileName = "user_events_outbox.json";

        // Background time after which the session ends.
        static readonly TimeSpan BackgroundTimeout = TimeSpan.FromSeconds(30);

        // Persisted so the next launch can end a session the app was killed in.
        const string PrefSessionId = "UserEvents.SessionId";
        const string PrefSessionUserGuid = "UserEvents.SessionGuid";
        const string PrefLastSeenTicks = "UserEvents.LastSeenTicks";

        static UserEventManager? instance;

        // Open panels, oldest first. A close reuses the properties of its open so the server can
        // pair them. EndSession closes whatever is left.
        readonly List<IPanelEvent> openPanels = new();

        UserEventOutbox outbox = null!; // Set in Awake.
        string? sessionId;
        string? userGuid;
        DateTime? backgroundedAtUtc;
        bool isSending;

        /// <summary>Null before launch completes or after quitting.</summary>
        internal static UserEventManager? Instance => instance != null ? instance : null;

        // After scene load, so panels enabling at startup (before any session) are ignored.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void CreateAtLaunch()
        {
            var host = new GameObject(nameof(UserEventManager));
            DontDestroyOnLoad(host);
            host.AddComponent<UserEventManager>();
        }

        // ===================== Unity lifecycle =====================

        void Awake()
        {
            instance = this;
            outbox = UserEventOutbox.Load(Path.Combine(Application.persistentDataPath, OutboxFileName));
            EndSessionFromLastRun();
        }

        void Start() => _ = SendPeriodicallyAsync(destroyCancellationToken);

        /// <summary>
        /// Called with true when the app goes to the background, false when it returns.
        /// No code runs in the background, so the timeout is checked on return. The session then
        /// ends at the time the app was left, and a new session (with a new id) starts on the
        /// task list. <see cref="UserEvents.SessionTimedOut"/> tells the UI to show it.
        /// </summary>
        void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                backgroundedAtUtc = DateTime.UtcNow;
                SaveLastSeen(backgroundedAtUtc.Value);
                PlayerPrefs.Save();
                _ = SendAsync();
                return;
            }

            // Also called with false once at startup.
            if (backgroundedAtUtc is not DateTime leftAt)
            {
                return;
            }

            backgroundedAtUtc = null;

            if (sessionId is null || DateTime.UtcNow - leftAt < BackgroundTimeout)
            {
                return;
            }

            EndSession(QuitReason.Timeout, leftAt);
            StartSession();

            // Opened here as well as by the UI: if the task list is already showing, its
            // OnEnable does not run again.
            OpenPanel(new TaskSelectionPanelToggled());
            UserEvents.OnSessionTimedOut();
        }

        // Rarely called on phones. EndSessionFromLastRun covers killed apps.
        void OnApplicationQuit() => EndSession(QuitReason.Quit, DateTime.UtcNow);

        // ===================== Session =====================

        /// <summary>Starts a session and sends asa_session_started, unless one is running.</summary>
        internal void StartSession()
        {
            if (sessionId is not null)
            {
                return;
            }

            string guid = PlayerPrefs.GetString("UserGuid", "");
            if (guid.Length == 0)
            {
                Debug.LogWarning("[UserEvents] No session started: the learner has no guid yet.");
                return;
            }

            sessionId = Guid.NewGuid().ToString();
            userGuid = guid;

            PlayerPrefs.SetString(PrefSessionId, sessionId);
            PlayerPrefs.SetString(PrefSessionUserGuid, userGuid);
            PlayerPrefs.Save();

            Record(new AsaSessionStarted(), DateTime.UtcNow);
        }

        /// <summary>Closes open panels, sends asa_session_ended at <paramref name="atUtc"/>, and sends the queue.</summary>
        internal void EndSession(QuitReason reason, DateTime atUtc)
        {
            if (sessionId is null)
            {
                return;
            }

            for (int i = openPanels.Count - 1; i >= 0; i--)
            {
                Record(openPanels[i], atUtc, active: false);
            }

            openPanels.Clear();
            Record(new AsaSessionEnded(reason), atUtc);

            sessionId = null;
            userGuid = null;
            ClearSavedSession();

            _ = SendAsync();
        }

        /// <summary>Drops the session and all queued events unsent (account deletion).</summary>
        internal void Forget()
        {
            sessionId = null;
            userGuid = null;
            openPanels.Clear();
            outbox.Clear();
            outbox.Save();
            ClearSavedSession();
        }

        /// <summary>
        /// Ends a session left open by a killed or crashed run, as a timeout at the last-seen time.
        /// </summary>
        void EndSessionFromLastRun()
        {
            string id = PlayerPrefs.GetString(PrefSessionId, "");
            string guid = PlayerPrefs.GetString(PrefSessionUserGuid, "");

            if (id.Length == 0 || guid.Length == 0)
            {
                return;
            }

            bool hasLastSeen = long.TryParse(
                PlayerPrefs.GetString(PrefLastSeenTicks, ""),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out long ticks
            );

            sessionId = id;
            userGuid = guid;
            EndSession(
                QuitReason.Timeout,
                hasLastSeen && ticks <= DateTime.MaxValue.Ticks
                    ? new DateTime(ticks, DateTimeKind.Utc)
                    : DateTime.UtcNow
            );
        }

        void SaveLastSeen(DateTime atUtc)
        {
            if (sessionId is not null)
            {
                PlayerPrefs.SetString(PrefLastSeenTicks, atUtc.Ticks.ToString(CultureInfo.InvariantCulture));
            }
        }

        static void ClearSavedSession()
        {
            PlayerPrefs.DeleteKey(PrefSessionId);
            PlayerPrefs.DeleteKey(PrefSessionUserGuid);
            PlayerPrefs.DeleteKey(PrefLastSeenTicks);
            PlayerPrefs.Save();
        }

        // ===================== Recording =====================

        /// <summary>Records the panel as open, unless it already is with the same content.</summary>
        internal void OpenPanel(IPanelEvent panel)
        {
            if (sessionId is null)
            {
                return;
            }

            int index = openPanels.FindIndex(open => open.Name == panel.Name);
            if (index >= 0)
            {
                // Already open with the same content. Struct Equals compares all fields.
                if (openPanels[index].Equals(panel))
                {
                    return;
                }

                // Same panel, new content (e.g. the next task): close the old one first.
                Record(openPanels[index], DateTime.UtcNow, active: false);
                openPanels.RemoveAt(index);
            }

            if (Record(panel, DateTime.UtcNow, active: true))
            {
                openPanels.Add(panel);
            }
        }

        /// <summary>Records the open panel with this event name as closed, if there is one.</summary>
        internal void ClosePanel(string eventName)
        {
            int index = openPanels.FindIndex(open => open.Name == eventName);
            if (index < 0)
            {
                return;
            }

            Record(openPanels[index], DateTime.UtcNow, active: false);
            openPanels.RemoveAt(index);
        }

        /// <summary>Records a tap or scroll.</summary>
        internal void Track(IInteractionEvent interaction) => Record(interaction, DateTime.UtcNow);

        /// <summary>Validates the event and adds it to the outbox.</summary>
        /// <param name="active">Panels only: written as the <c>active</c> property.</param>
        /// <returns>Whether the event was queued.</returns>
        bool Record(IUserEvent userEvent, DateTime atUtc, bool? active = null)
        {
            if (sessionId is null || userGuid is null)
            {
                return false;
            }

            if (userEvent.Validate() is string problem)
            {
                Debug.LogWarning($"[UserEvents] {userEvent.Name} not recorded: {problem}");
                return false;
            }

            var properties = new JsonWriter().BeginObject();
            if (active is bool isActive)
            {
                properties.Bool("active", isActive);
            }

            userEvent.WriteProperties(properties);

            outbox.Add(
                new QueuedEvent
                {
                    userGuid = userGuid,
                    sessionId = sessionId,
                    appVersion = Application.version,
                    eventId = Guid.NewGuid().ToString(),
                    // UTC with milliseconds. The server requires a timezone.
                    occurredAt = atUtc.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture),
                    name = userEvent.Name,
                    propertiesJson = properties.EndObject().ToString(),
                }
            );
            outbox.Save();
            SaveLastSeen(atUtc);

            if (outbox.Count >= SendAtQueueSize)
            {
                _ = SendAsync();
            }

            return true;
        }

        // ===================== Sending =====================

        async Awaitable SendPeriodicallyAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (true)
                {
                    await SendAsync();
                    await Awaitable.WaitForSecondsAsync(SendEverySeconds, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Destroyed. Unsent events stay in the outbox.
            }
        }

        /// <summary>
        /// Sends batches until the outbox is empty or a send must be retried. One run at a time.
        /// </summary>
        async Awaitable SendAsync()
        {
            // Blank in a fresh clone: events stay queued.
            string url = Secret.ASA_EVENTS_URL;

            if (isSending || outbox.Count == 0 || string.IsNullOrEmpty(url))
            {
                return;
            }

            isSending = true;

            try
            {
                while (outbox.NextBatch(MaxBatchSize) is { Count: > 0 } batch)
                {
                    var (outcome, detail) = await UserEventSender.SendAsync(url, batch, destroyCancellationToken);

                    switch (outcome)
                    {
                        case SendOutcome.RetryLater:
                            Debug.LogWarning($"[UserEvents] Send failed, will retry: {detail}");
                            return;

                        case SendOutcome.Malformed:
                            Debug.LogError($"[UserEvents] Server rejected a batch as malformed; dropped. {detail}");
                            break;

                        case SendOutcome.Refused:
                            Debug.LogWarning($"[UserEvents] Server refused a batch (no consent or no user); dropped. {detail}");
                            break;
                    }

                    outbox.Remove(batch);
                    outbox.Save();
                }
            }
            catch (OperationCanceledException)
            {
                // Destroyed mid-send. The batch is resent next launch and deduplicated by event_id.
            }
            catch (Exception e)
            {
                // Nothing awaits this send, so an exception would otherwise be lost.
                Debug.LogException(e);
            }
            finally
            {
                isSending = false;
            }
        }
    }
}
