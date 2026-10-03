using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// The sticker-pack opening moment.
    ///
    ///   1. The foil pack drops in on spinning rays, a light band sweeping its face.
    ///   2. Tap — it trembles harder and harder.
    ///   3. It tears along a jagged seal: the strip flips away, the body falls, a flash and a
    ///      burst of house-colour confetti (tokens.json color.house: "confetti" is an allowed use).
    ///   4. The animated stickers fly out one by one into a fan, each with a rising pop, NEW on
    ///      the ones just earned.
    ///   5. COLLECT closes it.
    ///
    /// Built at runtime from the art baked into Resources/UI/Modals/StickerPackOpener.prefab by
    /// LudoNXStickerPackBuilder, so it fits any screen height. Works in canvas units of the
    /// ScreenManager canvas (1080 wide, height varies).
    /// </summary>
    public class StickerPackOpener : MonoBehaviour
    {
        [Header("Art (baked by LudoNXStickerPackBuilder)")]
        [Tooltip("Parallel to StickerPackService.Stickers.")]
        public Texture2D[] textures;
        public Sprite packFrame;
        public Sprite ribbon;
        public Sprite circle;
        public Sprite glow;
        public Sprite button;
        public Sprite newChip;
        public Material shine;
        public TMP_FontAsset font;

        private const float PW = 500f, PH = 776f, TearAt = 0.2f;
        private static readonly Color Cream = new Color32(255, 242, 220, 255);
        private static readonly Color Gold = new Color32(242, 181, 31, 255);
        private static readonly Color[] Confetti =
        {
            new Color32(0xE7, 0x37, 0x35, 255), new Color32(0x4D, 0xAE, 0x51, 255),
            new Color32(0x1B, 0x89, 0xE7, 255), new Color32(0xFB, 0xC0, 0x2B, 255),
        };

        private static StickerPackOpener _open;
        private static Texture2D _zigzag;

        private RectTransform _root, _pack, _top, _bottom, _rays, _stickerLayer;
        private Image _scrim, _burst;
        private CanvasGroup _title, _collect, _hint;
        private TextMeshProUGUI _titleText, _subText;
        private readonly List<Material> _shines = new List<Material>();
        private StickerPackService.Pack _p;
        private bool _fresh, _tapped, _canClose;
        private Action _closed;

        public static bool IsOpen => _open != null;

        /// <param name="fresh">True right after buying: stickers get the NEW chip.</param>
        public static void Open(StickerPackService.Pack pack, bool fresh, Action closed = null)
        {
            if (_open != null || pack == null) return;
            var host = ScreenManager.Instance != null ? ScreenManager.Instance.Canvas : null;
            var prefab = Resources.Load<StickerPackOpener>("UI/Modals/StickerPackOpener");
            if (host == null || prefab == null)
            {
                Debug.LogWarning("[Ludo NX] Sticker pack opener unavailable (no canvas or prefab).");
                closed?.Invoke();
                return;
            }
            _open = Instantiate(prefab, host.transform, false);
            _open.name = "StickerPackOpener";
            _open._p = pack;
            _open._fresh = fresh;
            _open._closed = closed;
            _open.Build();
            _open.StartCoroutine(_open.Run());
        }

        // ======================================================================================
        // layers
        // ======================================================================================

        private void Build()
        {
            _root = (RectTransform)transform;
            Fill(_root);
            var canvas = GetComponent<Canvas>();
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 950;   // above the top bar and nav bar canvases
            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();

            _scrim = Img("Scrim", _root, null, new Color(0.02f, 0.02f, 0.05f, 0f));
            Fill(_scrim.rectTransform);
            _scrim.raycastTarget = true;
            var tap = _scrim.gameObject.AddComponent<Button>();
            tap.transition = Selectable.Transition.None;
            tap.onClick.AddListener(OnTap);

            var foilLight = Color.Lerp(_p.foil, Color.white, 0.45f);
            var rays = Img("Rays", _root, glow, new Color(foilLight.r, foilLight.g, foilLight.b, 0f));
            _rays = rays.rectTransform;
            Centre(_rays, 0f, 40f, 1250f, 1250f);

            _burst = Img("Burst", _root, circle, new Color(1f, 1f, 1f, 0f));
            Centre(_burst.rectTransform, 0f, 40f + PH * (0.5f - TearAt), 300f, 300f);

            _pack = Rect("Pack", _root);
            Centre(_pack, 0f, 40f, PW, PH);

            var shadow = Img("Shadow", _pack, packFrame, new Color(0f, 0f, 0f, 0.35f));
            Centre(shadow.rectTransform, 14f, -20f, PW, PH);

            // The pack is two halves, each a full copy of the face behind its own mask, so the
            // tear can pull them apart with the art lined up exactly at the cut.
            _top = Rect("TopPart", _pack);
            _top.anchorMin = new Vector2(0f, 1f - TearAt); _top.anchorMax = Vector2.one;
            _top.offsetMin = _top.offsetMax = Vector2.zero;
            var topMask = Rect("Mask", _top); Fill(topMask); topMask.gameObject.AddComponent<RectMask2D>();
            var topFace = Rect("Face", topMask);
            topFace.anchorMin = topFace.anchorMax = new Vector2(0.5f, 1f); topFace.pivot = new Vector2(0.5f, 1f);
            topFace.sizeDelta = new Vector2(PW, PH); topFace.anchoredPosition = Vector2.zero;
            Face(topFace);
            TearEdge(_top, false);

            _bottom = Rect("BottomPart", _pack);
            _bottom.anchorMin = Vector2.zero; _bottom.anchorMax = new Vector2(1f, 1f - TearAt);
            _bottom.offsetMin = _bottom.offsetMax = Vector2.zero;
            var botMask = Rect("Mask", _bottom); Fill(botMask); botMask.gameObject.AddComponent<RectMask2D>();
            var botFace = Rect("Face", botMask);
            botFace.anchorMin = botFace.anchorMax = new Vector2(0.5f, 0f); botFace.pivot = new Vector2(0.5f, 0f);
            botFace.sizeDelta = new Vector2(PW, PH); botFace.anchoredPosition = Vector2.zero;
            Face(botFace);
            TearEdge(_bottom, true);

            _hint = Group("TapHint", _root);
            Centre((RectTransform)_hint.transform, 0f, 40f - PH / 2f - 90f, 700f, 70f);
            Label((RectTransform)_hint.transform, "TAP TO OPEN", 46f, Cream);

            _stickerLayer = Rect("Stickers", _root);
            Fill(_stickerLayer);

            _title = Group("Title", _root);
            var titleRt = (RectTransform)_title.transform;
            titleRt.anchorMin = titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.sizeDelta = new Vector2(1000f, 200f);
            titleRt.anchoredPosition = new Vector2(0f, -330f);
            _titleText = Label(titleRt, _p.name, 78f, Cream);
            _titleText.rectTransform.anchoredPosition = new Vector2(0f, 30f);
            _subText = Label(titleRt, "", 36f, Gold);
            _subText.rectTransform.anchoredPosition = new Vector2(0f, -45f);

            _collect = Group("Collect", _root);
            var col = (RectTransform)_collect.transform;
            col.anchorMin = col.anchorMax = new Vector2(0.5f, 0f);
            col.sizeDelta = new Vector2(460f, 130f);
            col.anchoredPosition = new Vector2(0f, 330f);
            var colImg = col.gameObject.AddComponent<Image>();
            colImg.sprite = button;
            colImg.type = button != null && button.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            colImg.pixelsPerUnitMultiplier = 0.8f;
            var colBtn = col.gameObject.AddComponent<Button>();
            colBtn.onClick.AddListener(Close);
            Label(col, "COLLECT", 50f, Color.white).rectTransform.anchoredPosition = new Vector2(0f, 4f);

            _title.alpha = 0f; _collect.alpha = 0f; _hint.alpha = 0f;
            _collect.blocksRaycasts = false;
        }

        /// <summary>The printed face of the foil pack.</summary>
        private void Face(RectTransform face)
        {
            var body = Img("Body", face, packFrame, Color.Lerp(Color.white, _p.foil, 0.82f));
            Fill(body.rectTransform);
            if (shine != null)
            {
                var m = new Material(shine) { name = "PackShine" };
                m.SetVector("_UVRect", new Vector4(0f, 0f, 1f, 1f));
                m.SetFloat("_ShinePos", -1f);
                m.SetFloat("_ShineWidth", 0.14f);
                m.SetFloat("_ShineIntensity", 0.55f);
                body.material = m;
                _shines.Add(m);
            }

            // Crimped seals, top and bottom.
            var dark = Color.Lerp(_p.foil, Color.black, 0.35f);
            Crimp(face, true, dark);
            Crimp(face, false, dark);

            var brand = Label(face, "LUDO NX", 34f, Cream);
            Top(brand.rectTransform, 0.085f, 60f);
            var rarity = Label(face, _p.rarity.ToUpperInvariant(), 26f, Color.Lerp(Cream, _p.foil, 0.25f));
            Top(rarity.rectTransform, 0.135f, 40f);

            // Hero sticker on a white die-cut disc.
            var heroId = _p.stickers.Length > 0 ? _p.stickers[0] : null;
            var disc = Img("HeroDisc", face, circle, Color.white);
            Top(disc.rectTransform, 0.42f, 330f, 330f);
            var inner = Img("HeroInner", face, circle, Cream);
            Top(inner.rectTransform, 0.42f, 306f, 306f);
            var hero = StickerImage(face, heroId, 250f);
            if (hero != null) Top(hero, 0.42f, 250f, 250f);

            var rib = Img("Ribbon", face, ribbon, Color.white);
            Top(rib.rectTransform, 0.70f, 104f, 440f);
            var ribText = Label(rib.rectTransform, _p.name, 44f, Color.white);
            ribText.rectTransform.anchoredPosition = new Vector2(0f, 2f);

            var count = Label(face, _p.stickers.Length + " ANIMATED STICKERS", 28f, Cream);
            Top(count.rectTransform, 0.80f, 40f);
        }

        private void Crimp(RectTransform face, bool top, Color colour)
        {
            var band = new GameObject(top ? "CrimpTop" : "CrimpBottom", typeof(RectTransform), typeof(RawImage));
            var rt = (RectTransform)band.transform;
            rt.SetParent(face, false);
            rt.anchorMin = new Vector2(0f, top ? 1f : 0f);
            rt.anchorMax = new Vector2(1f, top ? 1f : 0f);
            rt.pivot = new Vector2(0.5f, top ? 1f : 0f);
            rt.sizeDelta = new Vector2(0f, 34f);
            var raw = band.GetComponent<RawImage>();
            raw.texture = Zigzag;
            raw.color = colour;
            raw.raycastTarget = false;
            // Teeth face into the pack; top band flips the texture vertically.
            raw.uvRect = top ? new Rect(0f, 1f, PW / 26f, -1f) : new Rect(0f, 0f, PW / 26f, 1f);
        }

        /// <summary>The ragged torn edge, drawn outside the mask so it hangs past the cut.</summary>
        private void TearEdge(RectTransform part, bool atTop)
        {
            var go = new GameObject("TearEdge", typeof(RectTransform), typeof(RawImage));
            var rt = (RectTransform)go.transform;
            rt.SetParent(part, false);
            rt.anchorMin = new Vector2(0f, atTop ? 1f : 0f);
            rt.anchorMax = new Vector2(1f, atTop ? 1f : 0f);
            rt.pivot = new Vector2(0.5f, atTop ? 0f : 1f);
            rt.sizeDelta = new Vector2(0f, 20f);
            var raw = go.GetComponent<RawImage>();
            raw.texture = Zigzag;
            raw.color = Cream;
            raw.raycastTarget = false;
            raw.uvRect = atTop ? new Rect(0.5f, 0f, PW / 18f, 1f) : new Rect(0f, 1f, PW / 18f, -1f);
            go.SetActive(false);   // shown the moment the pack tears
        }

        private RectTransform StickerImage(RectTransform parent, string stickerId, float size)
        {
            int idx = StickerPackService.IndexOf(stickerId);
            if (idx < 0 || textures == null || idx >= textures.Length || textures[idx] == null) return null;
            var s = StickerPackService.Stickers[idx];
            var go = new GameObject("Sticker_" + s.id, typeof(RectTransform), typeof(Image));
            go.SetActive(false);   // configure before OnEnable slices the sheet
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.sizeDelta = new Vector2(size, size);
            go.GetComponent<Image>().raycastTarget = false;
            go.GetComponent<Image>().preserveAspect = true;
            var fb = go.AddComponent<EmoteFlipbook>();
            fb.texture = textures[idx];
            fb.frameCount = s.frames;
            fb.contentSize = s.content;
            fb.framesPerSecond = s.fps;
            fb.loop = true;
            go.SetActive(true);
            return rt;
        }

        // ======================================================================================
        // sequence
        // ======================================================================================

        private IEnumerator Run()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButton();
            StartCoroutine(Spin(_rays, 12f));   // tokens.json motion.victory.raysDegPerSec
            StartCoroutine(ShineLoop());

            // Drop in: from above, overshoot, settle (popupOpen curve, stretched for weight).
            float h = _root.rect.height;
            yield return Tween(0.42f, t =>
            {
                // A full takeover, like the Result screen: partial alpha reads far lighter than
                // asked in Linear colour space (bible 2.6) and let the shop's title fight ours.
                _scrim.color = new Color(0.02f, 0.02f, 0.05f, Mathf.Clamp01(t * 2f));
                float e = OutBack(t);
                _pack.anchoredPosition = new Vector2(0f, Mathf.LerpUnclamped(h * 0.7f, 40f, e));
                _pack.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpUnclamped(-10f, 0f, e));
                SetAlpha(_rays.GetComponent<Image>(), 0.55f * t);
            });

            // Idle until tapped: float, and the hint breathes.
            float t0 = Time.unscaledTime;
            while (!_tapped)
            {
                float k = Time.unscaledTime - t0;
                _pack.anchoredPosition = new Vector2(0f, 40f + Mathf.Sin(k * 2.2f) * 10f);
                _pack.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(k * 1.4f) * 2f);
                _hint.alpha = 0.55f + 0.45f * Mathf.Sin(k * 3.2f);
                yield return null;
            }
            _hint.alpha = 0f;

            // Tremble, harder and harder.
            yield return Tween(0.6f, t =>
            {
                float amp = Mathf.Lerp(2f, 16f, t * t);
                _pack.anchoredPosition = new Vector2(UnityEngine.Random.Range(-amp, amp), 40f + UnityEngine.Random.Range(-amp, amp) * 0.5f);
                _pack.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-amp, amp) * 0.35f);
                float s = 1f + 0.07f * t;
                _pack.localScale = new Vector3(s, s, 1f);
                SetAlpha(_rays.GetComponent<Image>(), Mathf.Lerp(0.55f, 1f, t));
            });
            _pack.localRotation = Quaternion.identity;

            // Tear.
            if (AudioManager.Instance != null) AudioManager.Instance.PlayCapture();
            _top.Find("TearEdge").gameObject.SetActive(true);
            _bottom.Find("TearEdge").gameObject.SetActive(true);
            StartCoroutine(Flash());
            StartCoroutine(ConfettiBurst());
            StartCoroutine(Tween(0.55f, t =>
            {
                float e = 1f - Mathf.Pow(1f - t, 2f);
                _top.anchoredPosition = new Vector2(Mathf.Lerp(0f, 380f, e), Mathf.Lerp(0f, 520f, e) - 900f * t * t);
                _top.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, -48f, e));
                SetGroupAlpha(_top, 1f - t);
            }));
            yield return new WaitForSecondsRealtime(0.12f);
            yield return Tween(0.5f, t =>
            {
                float e = t * t;
                _bottom.anchoredPosition = new Vector2(0f, Mathf.Lerp(0f, -h * 0.8f, e));
                _bottom.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, 7f, e));
                SetGroupAlpha(_bottom, 1f - e);
                SetAlpha(_pack.Find("Shadow").GetComponent<Image>(), 0.35f * (1f - t * 3f));
            });
            _pack.gameObject.SetActive(false);

            // Reveal.
            _subText.text = _fresh ? _p.stickers.Length + " NEW STICKERS" : "YOUR STICKERS";
            StartCoroutine(Tween(0.26f, t =>
            {
                _title.alpha = t;
                float s = Mathf.Lerp(0.86f, 1f, t) + Mathf.Sin(t * Mathf.PI) * 0.06f;
                _title.transform.localScale = new Vector3(s, s, 1f);
            }));

            var slots = Layout(_p.stickers.Length);
            for (int i = 0; i < _p.stickers.Length; i++)
            {
                StartCoroutine(PopSticker(_p.stickers[i], slots[i], i));
                yield return new WaitForSecondsRealtime(0.11f);
            }
            yield return new WaitForSecondsRealtime(0.4f);

            _canClose = true;
            _collect.blocksRaycasts = true;
            yield return Tween(0.2f, t => _collect.alpha = t);
            StartCoroutine(Breathe((RectTransform)_collect.transform));   // the one idling element now
        }

        private float _itemScale = 1.25f;

        private List<Vector2> Layout(int n)
        {
            var list = new List<Vector2>();
            int perRow = n <= 3 ? n : (n <= 6 ? 3 : 4);
            int rows = Mathf.CeilToInt(n / (float)perRow);
            // Four across only fits 1080 at the smaller size.
            _itemScale = perRow >= 4 ? 1f : 1.25f;
            float dx = perRow >= 4 ? 250f : 300f, dy = perRow >= 4 ? 310f : 360f;
            float y0 = 20f + (rows - 1) * dy / 2f;
            for (int i = 0; i < n; i++)
            {
                int r = i / perRow, c = i % perRow;
                int inRow = Mathf.Min(perRow, n - r * perRow);
                float x = (c - (inRow - 1) / 2f) * dx;
                list.Add(new Vector2(x, y0 - r * dy));
            }
            return list;
        }

        private IEnumerator PopSticker(string id, Vector2 target, int index)
        {
            var item = Rect("Item_" + id, _stickerLayer);
            Centre(item, 0f, 40f, 200f, 200f);

            var shadow = Img("Shadow", item, circle, new Color(0f, 0f, 0f, 0.3f));
            Centre(shadow.rectTransform, 6f, -10f, 188f, 188f);
            var rim = Img("Rim", item, circle, Color.white);
            Centre(rim.rectTransform, 0f, 0f, 188f, 188f);
            var disc = Img("Disc", item, circle, Cream);
            Centre(disc.rectTransform, 0f, 0f, 170f, 170f);
            var art = StickerImage(item, id, 150f);
            if (art != null) Centre(art, 0f, 0f, 150f, 150f);

            var s = StickerPackService.Find(id);
            var name = Label(item, s != null ? s.name : id, 26f, Cream);
            Centre(name.rectTransform, 0f, -122f, 240f, 36f);

            if (_fresh && newChip != null)
            {
                var chip = Img("New", item, newChip, Color.white);
                chip.type = newChip.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
                chip.pixelsPerUnitMultiplier = 0.8f;
                Centre(chip.rectTransform, 62f, 78f, 100f, 44f);
                Label(chip.rectTransform, "NEW", 24f, Color.white);
            }

            if (AudioManager.Instance != null) AudioManager.Instance.PlayHop(1f + index * 0.09f);
            float spin = UnityEngine.Random.Range(-30f, 30f), rest = UnityEngine.Random.Range(-6f, 6f);
            yield return Tween(0.38f, t =>
            {
                float e = OutBack(t);
                item.anchoredPosition = Vector2.LerpUnclamped(new Vector2(0f, 40f), target, e);
                float sc = Mathf.LerpUnclamped(0.25f, _itemScale, e);
                item.localScale = new Vector3(sc, sc, 1f);
                item.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpUnclamped(spin, rest, e));
            });
        }

        private IEnumerator Flash()
        {
            yield return Tween(0.32f, t =>
            {
                float s = Mathf.Lerp(0.3f, 4.2f, 1f - Mathf.Pow(1f - t, 3f));
                _burst.rectTransform.localScale = new Vector3(s, s, 1f);
                SetAlpha(_burst, t < 0.15f ? t / 0.15f : 1f - (t - 0.15f) / 0.85f);
            });
        }

        private IEnumerator ConfettiBurst()
        {
            const int n = 44;
            var bits = new RectTransform[n];
            var vel = new Vector2[n];
            var spin = new float[n];
            var imgs = new Image[n];
            Vector2 origin = new Vector2(0f, 40f + PH * (0.5f - TearAt));
            for (int i = 0; i < n; i++)
            {
                imgs[i] = Img("Confetti", _root, null, Confetti[i % Confetti.Length]);
                bits[i] = imgs[i].rectTransform;
                bits[i].sizeDelta = i % 3 == 0 ? new Vector2(16f, 16f) : new Vector2(14f, 26f);
                bits[i].anchoredPosition = origin + new Vector2(UnityEngine.Random.Range(-PW / 2f, PW / 2f), 0f);
                vel[i] = new Vector2(UnityEngine.Random.Range(-950f, 950f), UnityEngine.Random.Range(700f, 1600f));
                spin[i] = UnityEngine.Random.Range(-720f, 720f);
            }
            float life = 1.6f, t0 = Time.unscaledTime;
            while (Time.unscaledTime - t0 < life)
            {
                float dt = Time.unscaledDeltaTime, k = (Time.unscaledTime - t0) / life;
                for (int i = 0; i < n; i++)
                {
                    vel[i].y -= 2600f * dt;
                    vel[i].x *= 1f - 1.2f * dt;
                    bits[i].anchoredPosition += vel[i] * dt;
                    bits[i].Rotate(0f, 0f, spin[i] * dt);
                    SetAlpha(imgs[i], k < 0.7f ? 1f : 1f - (k - 0.7f) / 0.3f);
                }
                yield return null;
            }
            foreach (var b in bits) if (b != null) Destroy(b.gameObject);
        }

        private IEnumerator ShineLoop()
        {
            while (_pack != null && _pack.gameObject.activeSelf)
            {
                yield return Tween(0.9f, t => { foreach (var m in _shines) m.SetFloat("_ShinePos", Mathf.Lerp(-0.2f, 1.2f, t)); });
                foreach (var m in _shines) m.SetFloat("_ShinePos", -1f);
                yield return new WaitForSecondsRealtime(1.1f);
            }
        }

        private static IEnumerator Breathe(RectTransform rt)
        {
            // playIdle: 1.0 ↔ 1.03 over 1600 ms.
            float t0 = Time.unscaledTime;
            while (rt != null)
            {
                float k = (Mathf.Sin((Time.unscaledTime - t0) / 1.6f * Mathf.PI * 2f) + 1f) * 0.5f;
                float s = Mathf.Lerp(1f, 1.03f, k);
                rt.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
        }

        private static IEnumerator Spin(RectTransform rt, float degPerSec)
        {
            while (rt != null) { rt.Rotate(0f, 0f, -degPerSec * Time.unscaledDeltaTime); yield return null; }
        }

        private void OnTap()
        {
            if (!_tapped) { _tapped = true; return; }
            if (_canClose) Close();
        }

        private void Close()
        {
            if (!_canClose) return;
            _canClose = false;
            StartCoroutine(CloseRoutine());
        }

        private IEnumerator CloseRoutine()
        {
            // popupClose: fade + scale to 0.92, 160 ms.
            var cg = gameObject.GetComponent<CanvasGroup>();
            if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
            yield return Tween(0.16f, t =>
            {
                cg.alpha = 1f - t;
                float s = Mathf.Lerp(1f, 0.92f, t);
                _stickerLayer.localScale = new Vector3(s, s, 1f);
            });
            foreach (var m in _shines) if (m != null) Destroy(m);
            _open = null;
            var done = _closed;
            Destroy(gameObject);
            done?.Invoke();
        }

        private void OnDestroy() { if (_open == this) _open = null; }

        // ======================================================================================
        // helpers
        // ======================================================================================

        private static Texture2D Zigzag
        {
            get
            {
                if (_zigzag != null) return _zigzag;
                // One tooth per texture width; the RawImage uvRect repeats it along the edge.
                const int W = 32, H = 32;
                _zigzag = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, name = "Zigzag" };
                var px = new Color32[W * H];
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                    {
                        float tooth = (1f - Mathf.Abs((x + 0.5f) / W * 2f - 1f)) * H;   // triangle, peak mid
                        float a = Mathf.Clamp01(tooth - y + 0.5f);
                        px[y * W + x] = new Color32(255, 255, 255, (byte)(a * 255));
                    }
                _zigzag.SetPixels32(px);
                _zigzag.Apply();
                return _zigzag;
            }
        }

        private TextMeshProUGUI Label(RectTransform parent, string text, float size, Color colour)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(Mathf.Max(parent.rect.width, 400f), size * 1.4f);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = colour;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.raycastTarget = false;
            return tmp;
        }

        /// <summary>Place on the pack face by fraction of its height from the top.</summary>
        private static void Top(RectTransform rt, float fromTop, float h, float w = PW)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(0f, -PH * fromTop);
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        private static Image Img(string name, Transform parent, Sprite sprite, Color colour)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = colour;
            img.raycastTarget = false;
            return img;
        }

        private static CanvasGroup Group(string name, Transform parent)
        {
            var rt = Rect(name, parent);
            return rt.gameObject.AddComponent<CanvasGroup>();
        }

        private static void Centre(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, y);
        }

        private static void Fill(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void SetAlpha(Graphic g, float a)
        {
            if (g == null) return;
            var c = g.color; c.a = Mathf.Clamp01(a); g.color = c;
        }

        private static void SetGroupAlpha(RectTransform rt, float a)
        {
            var cg = rt.GetComponent<CanvasGroup>();
            if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = Mathf.Clamp01(a);
        }

        private static float OutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        private static IEnumerator Tween(float seconds, Action<float> step)
        {
            float t0 = Time.unscaledTime;
            while (true)
            {
                float t = Mathf.Clamp01((Time.unscaledTime - t0) / Mathf.Max(0.001f, seconds));
                step(t);
                if (t >= 1f) yield break;
                yield return null;
            }
        }
    }
}
