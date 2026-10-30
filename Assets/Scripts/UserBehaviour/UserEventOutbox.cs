#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SaySuomi.UserBehaviour
{
    /// <summary>A recorded event awaiting send. Batch fields are captured at record time.</summary>
    [Serializable]
    internal sealed class QueuedEvent
    {
        public string userGuid = "";
        public string sessionId = "";
        public string appVersion = "";

        // Reused on retries so the server stores the event once.
        public string eventId = "";
        public string occurredAt = "";
        public string name = "";
        public string propertiesJson = "{}";
    }

    /// <summary>On-device queue of unsent events, oldest first, persisted as JSON.</summary>
    internal sealed class UserEventOutbox
    {
        // Most events kept on the device. The oldest are dropped first.
        const int MaxEvents = 200;

        // JsonUtility can not serialize a bare list.
        [Serializable]
        sealed class Snapshot
        {
            /// <summary>The queued events, oldest first.</summary>
            public List<QueuedEvent> events = new();
        }

        readonly string path;
        readonly List<QueuedEvent> events;

        UserEventOutbox(string path, List<QueuedEvent> events)
        {
            this.path = path;
            this.events = events;
        }

        /// <summary>Number of unsent events.</summary>
        public int Count => events.Count;

        /// <summary>Loads the outbox, or starts empty if the file is missing or unreadable.</summary>
        public static UserEventOutbox Load(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    Snapshot? snapshot = JsonUtility.FromJson<Snapshot>(File.ReadAllText(path));
                    return new UserEventOutbox(path, snapshot?.events ?? new List<QueuedEvent>());
                }
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException)
            {
                Debug.LogWarning($"[UserEvents] Outbox could not be read; starting empty. {e.Message}");
            }

            return new UserEventOutbox(path, new List<QueuedEvent>());
        }

        /// <summary>Appends an event, dropping the oldest past the cap.</summary>
        public void Add(QueuedEvent queued)
        {
            events.Add(queued);

            if (events.Count > MaxEvents)
            {
                int dropped = events.Count - MaxEvents;
                events.RemoveRange(0, dropped);
                Debug.LogWarning($"[UserEvents] Outbox full ({MaxEvents}); dropped the {dropped} oldest event(s).");
            }
        }

        /// <summary>
        /// The oldest event plus later ones that can share its request, up to <paramref name="maxSize"/>.
        /// A request carries one guid, session and app version.
        /// </summary>
        public List<QueuedEvent> NextBatch(int maxSize)
        {
            if (events.Count == 0)
            {
                return new List<QueuedEvent>();
            }

            QueuedEvent first = events[0];
            return events
                .Where(e => e.userGuid == first.userGuid && e.sessionId == first.sessionId && e.appVersion == first.appVersion)
                .Take(maxSize)
                .ToList();
        }

        /// <summary>Removes sent events by id, since new ones may have been added meanwhile.</summary>
        public void Remove(List<QueuedEvent> batch)
        {
            var sentIds = new HashSet<string>(batch.Select(e => e.eventId));
            events.RemoveAll(e => sentIds.Contains(e.eventId));
        }

        /// <summary>Removes every event. Call <see cref="Save"/> to persist.</summary>
        public void Clear() => events.Clear();

        /// <summary>Writes the outbox to disk.</summary>
        public void Save()
        {
            try
            {
                File.WriteAllText(path, JsonUtility.ToJson(new Snapshot { events = events }));
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // Still in memory. Saved again with the next event.
                Debug.LogWarning($"[UserEvents] Outbox could not be saved. {e.Message}");
            }
        }
    }
}
