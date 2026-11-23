using System;
using System.Collections;
using UnityEngine;

namespace SaySuomi.Navigation
{
    /// <summary>Animation curves and durations. See docs/NEW/TRANSITIONS.md.</summary>
    internal static class ScreenTransitions
    {
        public const float ScreenSeconds = 0.25f;
        public const float TabSeconds = 0.15f;
        public const float ModalSeconds = 0.2f;

        private const float ModalStartScale = 0.95f;

        public static Action<float> Slide(AppScreen from, AppScreen to, bool forward)
        {
            float direction = forward ? 1f : -1f;
            float width = to.Rect.rect.width;

            return t =>
            {
                to.Rect.anchoredPosition =
                    to.HomePosition + new Vector2(direction * width * (1f - t), 0f);
                from.Rect.anchoredPosition =
                    from.HomePosition - new Vector2(direction * width * t, 0f);
            };
        }

        public static Action<float> SlideVertical(AppScreen screen, bool opening)
        {
            float height = screen.Rect.rect.height;

            return t =>
            {
                float hidden = opening ? 1f - t : t;
                screen.Rect.anchoredPosition =
                    screen.HomePosition - new Vector2(0f, height * hidden);
            };
        }

        public static Action<float> Crossfade(AppScreen from, AppScreen to)
        {
            return t =>
            {
                to.Group.alpha = t;
                from.Group.alpha = 1f - t;
            };
        }

        public static Action<float> Modal(ModalScreen modal, bool opening)
        {
            return t =>
            {
                float shown = opening ? t : 1f - t;
                modal.Group.alpha = shown;
                modal.Rect.localScale = Vector3.one * Mathf.Lerp(ModalStartScale, 1f, shown);
            };
        }
    }

    /// <summary>
    /// Plays one transition at a time, always reaching its end state, even when interrupted.
    /// </summary>
    internal sealed class TransitionRunner
    {
        // Longest step per frame, so a heavy screen's first frame does not skip the animation.
        private const float MaxStepSeconds = 1f / 30f;

        private readonly MonoBehaviour host;
        private Coroutine running;
        private Action finish;

        public TransitionRunner(MonoBehaviour host)
        {
            this.host = host;
        }

        public bool IsRunning => running != null;

        public void Play(float seconds, Action<float> apply, Action onFinish)
        {
            Snap();

            if (seconds <= 0f)
            {
                onFinish();
                return;
            }

            finish = onFinish;
            apply(0f);
            running = host.StartCoroutine(Drive(seconds, apply));
        }

        /// <summary>Jumps a running transition to its end state.</summary>
        public void Snap()
        {
            if (running == null)
            {
                return;
            }

            host.StopCoroutine(running);
            Complete();
        }

        private IEnumerator Drive(float seconds, Action<float> apply)
        {
            // One frame first, so a screen that builds rows in OnEnable has done so.
            yield return null;

            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Mathf.Min(Time.unscaledDeltaTime, MaxStepSeconds);
                float progress = Mathf.Clamp01(elapsed / seconds);
                apply(1f - (1f - progress) * (1f - progress) * (1f - progress));
                yield return null;
            }

            Complete();
        }

        private void Complete()
        {
            running = null;
            Action onFinish = finish;
            finish = null;
            onFinish();
        }
    }
}
