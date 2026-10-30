#nullable enable

using System.Collections.Generic;
using System.Text;
using System.Threading;
using SaySuomi.Utils;
using UnityEngine;
using UnityEngine.Networking;

namespace SaySuomi.UserBehaviour
{
    /// <summary>How to handle a batch after sending (USER_EVENTS.md, Client column).</summary>
    internal enum SendOutcome
    {
        /// <summary>2xx. Invalid events are dropped server-side.</summary>
        Delivered,

        /// <summary>400/413/415/422 or other 4xx: client bug. Drop.</summary>
        Malformed,

        /// <summary>403/404/409: no consent, no user, or user deleted. Drop.</summary>
        Refused,

        /// <summary>5xx, 429 or no connection. Resend later.</summary>
        RetryLater,
    }

    /// <summary>Posts one batch to POST /analytics/events.</summary>
    internal static class UserEventSender
    {
        // Short: events are not urgent, and a hung send blocks the next.
        const int TimeoutSeconds = 20;

        /// <summary>Posts the batch as JSON.</summary>
        /// <returns>The outcome, plus status and body for logging.</returns>
        public static async Awaitable<(SendOutcome Outcome, string Detail)> SendAsync(
            string url,
            List<QueuedEvent> batch,
            CancellationToken cancellationToken
        )
        {
            using var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(BuildBody(batch)))
                {
                    contentType = "application/json",
                },
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = TimeoutSeconds,
            };

            using (cancellationToken.Register(request.Abort))
            {
                await request.SendWebRequest();
            }

            cancellationToken.ThrowIfCancellationRequested();

            string detail = $"{request.responseCode} {request.error} {request.downloadHandler.text}";
            return (Classify(request), detail);
        }

        /// <summary>
        /// Uses the guid, session and app version of the first event, which the whole batch
        /// shares (see <see cref="UserEventOutbox.NextBatch"/>).
        /// </summary>
        static string BuildBody(List<QueuedEvent> batch)
        {
            QueuedEvent first = batch[0];

            var json = new JsonWriter()
                .BeginObject()
                .String("guid", first.userGuid)
                .String("asa_session_id", first.sessionId)
                .String("app_version", first.appVersion)
                .BeginArray("events");

            foreach (QueuedEvent queued in batch)
            {
                json.BeginObject()
                    .String("event_id", queued.eventId)
                    .String("occurred_at", queued.occurredAt)
                    .String("name", queued.name)
                    .Raw("properties", queued.propertiesJson)
                    .EndObject();
            }

            return json.EndArray().EndObject().ToString();
        }

        static SendOutcome Classify(UnityWebRequest request)
        {
            // No response at all (offline, DNS, timeout).
            if (request.result == UnityWebRequest.Result.ConnectionError || request.responseCode == 0)
            {
                return SendOutcome.RetryLater;
            }

            return request.responseCode switch
            {
                >= 200 and < 300 => SendOutcome.Delivered,
                403 or 404 or 409 => SendOutcome.Refused,
                429 or >= 500 => SendOutcome.RetryLater,

                // Other 4xx: resending can not succeed, so drop rather than block the outbox.
                _ => SendOutcome.Malformed,
            };
        }
    }
}
