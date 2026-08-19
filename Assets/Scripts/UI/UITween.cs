using System;
using System.Collections;
using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// A small tweening helper for interface motion.
    ///
    /// Deliberately not a tweening library. The project has no DOTween and does not need one —
    /// UI motion here is a handful of durations and four easing curves. This runs on its own
    /// hidden runner so a tween survives the object that started it being hidden mid-transition,
    /// which is exactly what happens when one screen fades out while pushing another.
    ///
    /// All tweens use unscaled time. Menus must keep animating while the match is paused.
    /// </summary>
    public static class UITween
    {
        private class Runner : MonoBehaviour { }

        private static Runner _runner;

        internal static void ResetStatics() { _runner = null; }

        private static Runner Host
        {
            get
            {
                if (_runner != null) return _runner;
                var go = new GameObject("~UITweenRunner") { hideFlags = HideFlags.HideAndDontSave };
                UnityEngine.Object.DontDestroyOnLoad(go);
                _runner = go.AddComponent<Runner>();
                return _runner;
            }
        }

        // ── Easing ────────────────────────────────────────────────────────────────────────

        public static float Linear(float t) => t;

        public static float OutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

        public static float InOutCubic(float t) =>
            t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;

        /// <summary>
        /// Very soft deceleration — fast at the start, long gentle settle, never overshoots.
        /// This is the house curve for anything the player triggers by touching it.
        /// </summary>
        public static float OutQuint(float t) => 1f - Mathf.Pow(1f - t, 5f);

        /// <summary>Smooth both ends, no overshoot. For things that move on their own.</summary>
        public static float Smooth(float t) => t * t * (3f - 2f * t);

        // ── Retired curves ────────────────────────────────────────────────────────────────
        // OutBack (overshoot-and-settle) and OutSpring (damped oscillation) used to drive the
        // button release, screen pushes and toggle thumbs. Both cross their target value and
        // come back, and on a small element over ~150ms that reads as a shake rather than as
        // bounce. They are kept only so older references still compile; nothing uses them.

        [System.Obsolete("Overshoots — reads as a shake. Use OutQuint or OutCubic.")]
        public static float OutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float p = t - 1f;
            return 1f + c3 * p * p * p + c1 * p * p;
        }

        [System.Obsolete("Oscillates — this was the shake. Use OutQuint.")]
        public static float OutSpring(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            return 1f - Mathf.Cos(t * Mathf.PI * 3.4f) * Mathf.Exp(-t * 6.2f);
        }

        // ── Core ──────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Drives <paramref name="onStep"/> from 0 to 1 over <paramref name="duration"/>, eased.
        /// Always fires exactly 1.0 on the final frame so tweens land on their target value.
        /// </summary>
        public static Coroutine Run(float duration, Func<float, float> ease, Action<float> onStep, Action onDone = null)
        {
            if (onStep == null) return null;
            if (duration <= 0f)
            {
                onStep(1f);
                onDone?.Invoke();
                return null;
            }
            return Host.StartCoroutine(Drive(duration, ease ?? OutCubic, onStep, onDone));
        }

        public static void Stop(Coroutine c)
        {
            if (c != null && _runner != null) _runner.StopCoroutine(c);
        }

        private static IEnumerator Drive(float duration, Func<float, float> ease, Action<float> onStep, Action onDone)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                onStep(ease(t));
                if (t >= 1f) break;
                yield return null;
            }
            onStep(1f);
            onDone?.Invoke();
        }

        // ── Common shorthands ─────────────────────────────────────────────────────────────

        public static Coroutine Fade(CanvasGroup group, float to, float duration, Action onDone = null)
        {
            if (group == null) return null;
            float from = group.alpha;
            return Run(duration, OutCubic, t => group.alpha = Mathf.LerpUnclamped(from, to, t), onDone);
        }

        public static Coroutine Scale(Transform target, Vector3 to, float duration,
                                      Func<float, float> ease = null, Action onDone = null)
        {
            if (target == null) return null;
            Vector3 from = target.localScale;
            return Run(duration, ease ?? OutQuint, t => target.localScale = Vector3.LerpUnclamped(from, to, t), onDone);
        }

        public static Coroutine MoveAnchored(RectTransform target, Vector2 to, float duration,
                                             Func<float, float> ease = null, Action onDone = null)
        {
            if (target == null) return null;
            Vector2 from = target.anchoredPosition;
            return Run(duration, ease ?? OutQuint,
                       t => target.anchoredPosition = Vector2.LerpUnclamped(from, to, t), onDone);
        }

        /// <summary>Counts a number up for result screens. Integers only — a rolling decimal reads as noise.</summary>
        public static Coroutine CountTo(int from, int to, float duration, Action<int> onStep, Action onDone = null)
        {
            return Run(duration, OutCubic,
                       t => onStep(Mathf.RoundToInt(Mathf.LerpUnclamped(from, to, t))), onDone);
        }
    }
}
