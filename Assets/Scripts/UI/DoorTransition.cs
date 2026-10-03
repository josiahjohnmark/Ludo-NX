using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// The vault doors. Two halves of the NX emboss slide shut over whatever is showing, slam
    /// with a jolt, hold while the destination is swapped in behind them, then slide apart.
    ///
    /// Owner decision, 27 Sep 2026: the doors are the entrance to LEVELS. (Bible 8.2 reserved
    /// them for match entry; the owner overrode that — recorded in AI_HANDOFF DECISIONS.)
    ///
    /// Timings come from tokens.json motion.doors (close 350 ms ease-in, open 450 ms ease-out)
    /// plus the ~500 ms minimum hold from SCREEN_PLANS/screens/match-intro-doors.json so a fast
    /// swap never flickers.
    ///
    /// The prefab (Resources/UI/Components/DoorTransition.prefab) only carries the art
    /// references; the layers are built here so the doors always fit the live screen size.
    /// When dedicated door art exists, assign it to <see cref="doorTexture"/> — nothing else changes.
    /// </summary>
    public class DoorTransition : MonoBehaviour
    {
        [Header("Art")]
        [Tooltip("Split down the middle: the left half is the left door. Symmetric art reads best.")]
        public Texture doorTexture;
        [Tooltip("Sits on the seam, cut in half by the doors; whole only when they are shut.")]
        public Sprite emblem;
        [Tooltip("Round medallion the emblem is seated on; hides the emblem's soft alpha edge, which Linear colour space lifts into a grey box.")]
        public Sprite plate;

        [Header("Motion (tokens.json motion.doors)")]
        public float closeMs = 350f;
        public float openMs = 450f;
        public float minHoldMs = 500f;
        public float shakeMs = 220f;
        public float shakePoints = 10f;

        private static DoorTransition _instance;
        private RectTransform _root, _shake, _left, _right, _seam;
        private RawImage _leftImg, _rightImg;
        private Image _scrim, _seamImg;
        private RectTransform _emblemL, _emblemR;
        private readonly System.Collections.Generic.List<RectTransform> _plates = new System.Collections.Generic.List<RectTransform>();
        private readonly System.Collections.Generic.List<Image> _casts = new System.Collections.Generic.List<Image>();
        private bool _running;

        public static bool IsPlaying => _instance != null && _instance._running;

        /// <summary>
        /// Close the doors, run <paramref name="whileClosed"/> behind them, open again.
        /// Falls back to calling it immediately if the doors cannot be shown.
        /// </summary>
        public static void Play(Action whileClosed)
        {
            if (IsPlaying) return;
            var host = ScreenManager.Instance != null ? ScreenManager.Instance.Canvas : null;
            if (host == null) { whileClosed?.Invoke(); return; }

            if (_instance == null)
            {
                var prefab = Resources.Load<DoorTransition>("UI/Components/DoorTransition");
                if (prefab == null) { whileClosed?.Invoke(); return; }
                _instance = Instantiate(prefab, host.transform, false);
                _instance.name = "DoorTransition";
                _instance.Build();
            }
            _instance.transform.SetAsLastSibling();
            _instance.gameObject.SetActive(true);
            _instance.StartCoroutine(_instance.Run(whileClosed));
        }

        // ======================================================================================
        // layers
        // ======================================================================================

        private void Build()
        {
            _root = (RectTransform)transform;
            Fill(_root);

            // Above every screen, popup and the nav bar; eats taps while it runs.
            var canvas = GetComponent<Canvas>();
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 900;
            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();

            _scrim = NewImage("Scrim", _root);
            Fill(_scrim.rectTransform);
            _scrim.color = new Color(0f, 0f, 0f, 0f);
            _scrim.raycastTarget = true;

            _shake = NewRect("Shake", _root);
            Fill(_shake);

            _left = Door("LeftDoor", 0f, out _leftImg, out _emblemL);
            _right = Door("RightDoor", 1f, out _rightImg, out _emblemR);

            _seamImg = NewImage("SeamFlash", _shake);
            _seam = _seamImg.rectTransform;
            _seam.anchorMin = new Vector2(0.5f, 0f);
            _seam.anchorMax = new Vector2(0.5f, 1f);
            _seam.sizeDelta = new Vector2(8f, 0f);
            _seamImg.color = new Color(1f, 0.93f, 0.7f, 0f);
            _seamImg.raycastTarget = false;
        }

        /// <param name="side">0 = left door, 1 = right door.</param>
        private RectTransform Door(string name, float side, out RawImage img, out RectTransform emblemRt)
        {
            var rt = NewRect(name, _shake);
            rt.anchorMin = new Vector2(side * 0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f + side * 0.5f, 1f);
            rt.pivot = new Vector2(1f - side, 0.5f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.gameObject.AddComponent<RectMask2D>();

            img = rt.gameObject.AddComponent<RawImage>();
            img.texture = doorTexture;
            img.color = doorTexture != null ? Color.white : new Color32(40, 45, 56, 255);
            img.raycastTarget = true;

            // Inner-edge shade: the lip where the two doors meet, so they read as thick slabs.
            var lip = NewImage("Lip", rt);
            lip.rectTransform.anchorMin = new Vector2(1f - side, 0f);
            lip.rectTransform.anchorMax = new Vector2(1f - side, 1f);
            lip.rectTransform.pivot = new Vector2(1f - side, 0.5f);
            lip.rectTransform.sizeDelta = new Vector2(6f, 0f);
            lip.color = new Color(0f, 0f, 0f, 0.45f);
            lip.raycastTarget = false;

            // Leading-edge shadow cast onto the screen still visible beside the door.
            var cast = NewImage("CastShadow", rt);
            cast.rectTransform.anchorMin = new Vector2(1f - side, 0f);
            cast.rectTransform.anchorMax = new Vector2(1f - side, 1f);
            cast.rectTransform.pivot = new Vector2(side, 0.5f);
            cast.rectTransform.sizeDelta = new Vector2(18f, 0f);
            cast.color = new Color(0f, 0f, 0f, 0.28f);
            cast.raycastTarget = false;
            cast.maskable = false;
            _casts.Add(cast);

            emblemRt = null;
            if (plate != null)
            {
                // Bronze rim, then the dark face — both halves meet at the seam like the emblem.
                var rim = NewImage("PlateRim", rt);
                rim.sprite = plate;
                rim.color = new Color32(122, 84, 52, 255);
                var face = NewImage("PlateFace", rt);
                face.sprite = plate;
                face.color = new Color32(18, 22, 34, 255);
                foreach (var img2 in new[] { rim, face })
                {
                    img2.raycastTarget = false;
                    var p = img2.rectTransform;
                    p.anchorMin = p.anchorMax = new Vector2(1f - side, 0.5f);
                    p.pivot = new Vector2(0.5f, 0.5f);
                    p.anchoredPosition = Vector2.zero;
                    _plates.Add(p);
                }
            }
            if (emblem != null)
            {
                var e = NewImage("EmblemHalf", rt);
                e.sprite = emblem;
                e.preserveAspect = true;
                e.raycastTarget = false;
                emblemRt = e.rectTransform;
                emblemRt.anchorMin = emblemRt.anchorMax = new Vector2(1f - side, 0.5f);
                emblemRt.pivot = new Vector2(0.5f, 0.5f);
                emblemRt.anchoredPosition = Vector2.zero;
            }
            return rt;
        }

        /// <summary>Crop the texture to cover the live screen, split exactly at its centre line.</summary>
        private void FitArt()
        {
            float w = _root.rect.width, h = _root.rect.height;
            if (w <= 0f || h <= 0f) return;

            if (doorTexture != null)
            {
                float texAspect = doorTexture.width / (float)doorTexture.height;
                float scrAspect = w / h;
                float uw = 1f, uh = 1f;
                if (scrAspect < texAspect) uw = scrAspect / texAspect; else uh = texAspect / scrAspect;
                float y = 0.5f - uh / 2f;
                _leftImg.uvRect = new Rect(0.5f - uw / 2f, y, uw / 2f, uh);
                _rightImg.uvRect = new Rect(0.5f, y, uw / 2f, uh);
            }

            float size = Mathf.Min(w * 0.46f, h * 0.26f);
            if (_emblemL != null) _emblemL.sizeDelta = new Vector2(size, size);
            if (_emblemR != null) _emblemR.sizeDelta = new Vector2(size, size);
            // Plates are added rim, face per door: rim a touch larger than the face.
            for (int i = 0; i < _plates.Count; i++)
            {
                float d = size * (i % 2 == 0 ? 1.5f : 1.44f);
                _plates[i].sizeDelta = new Vector2(d, d);
            }
        }

        // ======================================================================================
        // sequence
        // ======================================================================================

        private IEnumerator Run(Action whileClosed)
        {
            _running = true;
            Canvas.ForceUpdateCanvases();
            FitArt();

            float half = _root.rect.width * 0.5f + 24f;   // + the cast shadow, fully off screen
            SetOpen(half);
            _scrim.color = new Color(0f, 0f, 0f, 0f);
            _shake.anchoredPosition = Vector2.zero;

            if (AudioManager.Instance != null) AudioManager.Instance.PlayButton();

            // Close — ease-in, so the doors gather speed and hit hard.
            yield return Tween(closeMs, t =>
            {
                float e = t * t * t;
                SetOpen(Mathf.LerpUnclamped(half, 0f, e));
                _scrim.color = new Color(0f, 0f, 0f, 0.45f * t);
            });
            SetOpen(0f);

            // Slam.
            float held = 0f;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayCapture();
            StartCoroutine(Punch());
            StartCoroutine(SeamFlash());
            yield return Shake();
            held += shakeMs;

            // Swap the destination in behind the closed doors.
            try { whileClosed?.Invoke(); }
            catch (Exception ex) { Debug.LogException(ex); }

            float wait = Mathf.Max(0f, minHoldMs - held) / 1000f;
            float end = Time.unscaledTime + wait;
            while (Time.unscaledTime < end) yield return null;

            // Open — ease-out, a heavy door that glides to rest.
            yield return Tween(openMs, t =>
            {
                float e = 1f - Mathf.Pow(1f - t, 3f);
                SetOpen(Mathf.LerpUnclamped(0f, half, e));
                _scrim.color = new Color(0f, 0f, 0f, 0.45f * (1f - t));
            });

            _running = false;
            gameObject.SetActive(false);
        }

        private void SetOpen(float offset)
        {
            _left.anchoredPosition = new Vector2(-offset, 0f);
            _right.anchoredPosition = new Vector2(offset, 0f);
            // Shadows only while there is a gap to cast into; shut doors meet cleanly.
            float a = 0.28f * Mathf.Clamp01(offset / 40f);
            foreach (var c in _casts) c.color = new Color(0f, 0f, 0f, a);
        }

        private IEnumerator Shake()
        {
            // A jolt, then a small rebound where the doors bounce apart and re-seat.
            float dur = shakeMs / 1000f, t0 = Time.unscaledTime;
            while (Time.unscaledTime - t0 < dur)
            {
                float k = (Time.unscaledTime - t0) / dur;
                float amp = shakePoints * (1f - k) * (1f - k);
                _shake.anchoredPosition = new Vector2(UnityEngine.Random.Range(-amp, amp), UnityEngine.Random.Range(-amp, amp) * 0.6f);
                float rebound = Mathf.Sin(k * Mathf.PI) * 12f * (1f - k);
                SetOpen(rebound);
                yield return null;
            }
            _shake.anchoredPosition = Vector2.zero;
            SetOpen(0f);
        }

        private IEnumerator Punch()
        {
            if (_emblemL == null) yield break;
            yield return Tween(260f, t =>
            {
                // 1 → 1.16 → 1, overshoot early.
                float s = 1f + 0.16f * Mathf.Sin(Mathf.Clamp01(t * 1.4f) * Mathf.PI) * (1f - t * 0.3f);
                _emblemL.localScale = _emblemR.localScale = new Vector3(s, s, 1f);
            });
            _emblemL.localScale = _emblemR.localScale = Vector3.one;
        }

        private IEnumerator SeamFlash()
        {
            yield return Tween(280f, t =>
            {
                float a = t < 0.2f ? t / 0.2f : 1f - (t - 0.2f) / 0.8f;
                var c = _seamImg.color; c.a = 0.9f * a; _seamImg.color = c;
                _seam.sizeDelta = new Vector2(Mathf.Lerp(14f, 3f, t), 0f);
            });
        }

        private static IEnumerator Tween(float ms, Action<float> step)
        {
            float dur = Mathf.Max(0.001f, ms / 1000f), t0 = Time.unscaledTime;
            while (true)
            {
                float t = Mathf.Clamp01((Time.unscaledTime - t0) / dur);
                step(t);
                if (t >= 1f) yield break;
                yield return null;
            }
        }

        // ======================================================================================

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        private static Image NewImage(string name, Transform parent)
        {
            var rt = NewRect(name, parent);
            return rt.gameObject.AddComponent<Image>();
        }

        private static void Fill(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }
    }
}
