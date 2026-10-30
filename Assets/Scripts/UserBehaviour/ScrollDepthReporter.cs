#nullable enable

using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SaySuomi.UserBehaviour
{
    /// <summary>
    /// Reports the depth of a ScrollRect once a user scroll comes to rest.
    ///
    /// Only drags and wheel scrolls count, not programmatic resets (e.g. FeedbackPanel.ScrollToTop).
    /// Lives on the GameObject of the ScrollRect so it receives the same drag events.
    /// </summary>
    /// <example>
    /// <code>
    /// void OnEnable()
    /// {
    ///     ScrollDepthReporter.Attach(
    ///         GetComponentInChildren&lt;ScrollRect&gt;(true),
    ///         depth => UserEvents.Track(new FeedbackPanelScrolled(depth, taskId))
    ///     );
    /// }
    /// </code>
    /// </example>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ScrollRect))]
    public sealed class ScrollDepthReporter
        : MonoBehaviour,
            IBeginDragHandler,
            IEndDragHandler,
            IScrollHandler
    {
        // ScrollRect zeroes inertia below 1 unit/s.
        const float RestingSpeedSqr = 1f;

        // Hold time before a scroll counts as at rest, so each wheel notch is not its own report.
        const float SettleSeconds = 0.3f;

        // Smaller changes are not reported.
        const float MinDepthChange = 0.01f;

        ScrollRect? scrollRect;
        Action<float>? report;
        bool isDragging;
        bool isArmed;
        float lastMovedAt;
        float lastReportedDepth = -1f;

        /// <summary>
        /// Reports depth (0 = top, 1 = bottom) to <paramref name="report"/>. Safe to call repeatedly.
        /// </summary>
        public static void Attach(ScrollRect? scrollRect, Action<float> report)
        {
            if (scrollRect == null)
            {
                Debug.LogWarning("[UserEvents] No ScrollRect to report scroll depth for.");
                return;
            }

            if (!scrollRect.TryGetComponent(out ScrollDepthReporter reporter))
            {
                reporter = scrollRect.gameObject.AddComponent<ScrollDepthReporter>();
            }

            reporter.scrollRect = scrollRect;
            reporter.report = report;
        }

        // Each opening starts fresh, so revisiting a depth is reported again.
        void OnEnable()
        {
            isDragging = false;
            isArmed = false;
            lastReportedDepth = -1f;
        }

        /// <summary>A user drag starts: arms the reporter.</summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            isDragging = true;
            Arm();
        }

        /// <summary>The drag ends. Inertia may still be moving the content.</summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
            lastMovedAt = Time.unscaledTime;
        }

        /// <summary>A mouse wheel or trackpad scroll: arms the reporter.</summary>
        public void OnScroll(PointerEventData eventData) => Arm();

        void Arm()
        {
            isArmed = true;
            lastMovedAt = Time.unscaledTime;
        }

        void LateUpdate()
        {
            if (!isArmed || isDragging || scrollRect == null)
            {
                return;
            }

            if (scrollRect.velocity.sqrMagnitude > RestingSpeedSqr)
            {
                lastMovedAt = Time.unscaledTime;
                return;
            }

            if (Time.unscaledTime - lastMovedAt < SettleSeconds)
            {
                return;
            }

            isArmed = false;

            // Unity runs from bottom (0) to top (1), and the server expects the reverse.
            // Clamped for elastic overscroll.
            float depth = Mathf.Clamp01(1f - scrollRect.verticalNormalizedPosition);

            if (Mathf.Abs(depth - lastReportedDepth) < MinDepthChange)
            {
                return;
            }

            lastReportedDepth = depth;
            report?.Invoke(depth);
        }
    }
}
