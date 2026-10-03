using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// ALBUM — the player's collection, reached from the bottom nav. Replaces the older Clay
    /// Pawn Collection (same ScreenId, so the nav needs no change).
    ///
    /// Only real state is shown:
    ///   STICKERS — the 18 animated stickers; owned ones come from StickerPackService packs.
    ///   DICE     — the shop's 10 skins; CLASSIC is the one every player has. Dice purchases are
    ///              not wired yet (StoreScreen's dice popup is a placeholder), so the rest say IN SHOP.
    ///   TOKENS   — the four house pawns every player plays with.
    /// Tapping something locked opens the shop on the tab that sells it.
    /// </summary>
    public class AlbumScreen : UIScreen
    {
        public enum Tab { Stickers, Dice, Tokens }

        [Header("Header")]
        public Button backButton;

        [Header("Tabs (Stickers, Dice, Tokens)")]
        public Button[] tabButtons;
        public Image[] tabBackgrounds;
        public TextMeshProUGUI[] tabLabels;
        public Sprite tabSelected, tabIdle;

        [Header("Progress")]
        public TextMeshProUGUI progressLabel;
        public RectTransform progressFill;

        [Header("Grid")]
        public ScrollRect scroll;
        public RectTransform gridContent;
        public RectTransform cellTemplate;
        public int columns = 4;
        public Vector2 cellPitch = new Vector2(88f, 120f);

        [Header("Art")]
        [Tooltip("Parallel to StickerPackService.Stickers.")]
        public Texture2D[] stickerTextures;
        public Sprite[] diceSprites;
        public string[] diceNames;
        public Sprite[] pawnSprites;
        public string[] pawnNames;

        private Tab _tab = Tab.Stickers;
        private readonly List<GameObject> _cells = new List<GameObject>();

        // Faded, not blacked out: the player should see exactly what they are missing.
        private static readonly Color LockedTint = new Color(0.62f, 0.58f, 0.54f, 0.5f);

        private void Awake()
        {
            if (backButton != null) backButton.onClick.AddListener(() => ScreenManager.Instance.Pop());
            if (cellTemplate != null) cellTemplate.gameObject.SetActive(false);
            if (tabButtons != null)
                for (int i = 0; i < tabButtons.Length; i++)
                {
                    if (tabButtons[i] == null) continue;
                    var t = (Tab)i;
                    tabButtons[i].onClick.AddListener(() => SetTab(t));
                }
        }

        public override void OnShow(object args)
        {
            var nav = ScreenManager.Instance != null ? ScreenManager.Instance.BottomNav : null;
            if (nav != null) nav.SetActiveTab(HomeNavBar.Tab.Album);
            SetTab(_tab);
        }

        private void SetTab(Tab tab)
        {
            _tab = tab;
            if (tabBackgrounds != null)
                for (int i = 0; i < tabBackgrounds.Length; i++)
                {
                    bool on = (int)tab == i;
                    if (tabBackgrounds[i] != null && tabSelected != null && tabIdle != null)
                        tabBackgrounds[i].sprite = on ? tabSelected : tabIdle;
                    if (tabLabels != null && i < tabLabels.Length && tabLabels[i] != null)
                        tabLabels[i].color = on ? new Color32(48, 32, 12, 255) : new Color32(244, 230, 203, 230);
                }

            Clear();
            switch (tab)
            {
                case Tab.Stickers: BuildStickers(); break;
                case Tab.Dice: BuildDice(); break;
                default: BuildTokens(); break;
            }
            if (scroll != null) scroll.verticalNormalizedPosition = 1f;
        }

        // ======================================================================================

        private void BuildStickers()
        {
            int owned = 0;
            var all = StickerPackService.Stickers;
            for (int i = 0; i < all.Length; i++)
            {
                var s = all[i];
                bool has = StickerPackService.OwnsSticker(s.id);
                if (has) owned++;
                var pack = PackFor(s.id);
                var cell = NewCell(i);
                var art = Child<Image>(cell, "Art");
                if (art != null && stickerTextures != null && i < stickerTextures.Length && stickerTextures[i] != null)
                {
                    var go = art.gameObject;
                    go.SetActive(false);   // configure before OnEnable slices the sheet
                    var fb = go.GetComponent<EmoteFlipbook>();
                    if (fb == null) fb = go.AddComponent<EmoteFlipbook>();   // no ?? on Unity objects
                    fb.texture = stickerTextures[i];
                    fb.frameCount = s.frames;
                    fb.contentSize = s.content;
                    fb.framesPerSecond = s.fps;
                    fb.loop = true;
                    go.SetActive(true);
                    fb.enabled = has;   // locked stickers hold still
                    art.color = has ? Color.white : LockedTint;
                }
                Fill(cell, s.name, has ? "OWNED" : (pack != null ? pack.name : "LOCKED"), has);
                if (!has) OnTap(cell, () => OpenShop(StoreScreen.Tab.Emoji,
                    pack != null ? "Find it in the " + pack.name : "Coming soon"));
            }
            SetProgress(owned, all.Length, "STICKERS");
        }

        private void BuildDice()
        {
            int n = diceSprites != null ? diceSprites.Length : 0;
            for (int i = 0; i < n; i++)
            {
                bool has = i == 0;   // CLASSIC; dice purchases are not wired yet
                var cell = NewCell(i);
                var art = Child<Image>(cell, "Art");
                if (art != null) { art.sprite = diceSprites[i]; art.color = has ? Color.white : LockedTint; }
                string name = diceNames != null && i < diceNames.Length ? diceNames[i] : "DICE";
                Fill(cell, name, has ? "OWNED" : "IN SHOP", has);
                if (!has) OnTap(cell, () => OpenShop(StoreScreen.Tab.Dice, name + " is in the Shop"));
            }
            SetProgress(n > 0 ? 1 : 0, n, "DICE");
        }

        private void BuildTokens()
        {
            int n = pawnSprites != null ? pawnSprites.Length : 0;
            for (int i = 0; i < n; i++)
            {
                var cell = NewCell(i);
                var art = Child<Image>(cell, "Art");
                if (art != null) { art.sprite = pawnSprites[i]; art.color = Color.white; }
                Fill(cell, pawnNames != null && i < pawnNames.Length ? pawnNames[i] : "TOKEN", "OWNED", true);
            }
            SetProgress(n, n, "TOKENS");
        }

        // ======================================================================================

        private static StickerPackService.Pack PackFor(string stickerId)
        {
            foreach (var p in StickerPackService.Packs)
                if (System.Array.IndexOf(p.stickers, stickerId) >= 0) return p;
            return null;
        }

        private RectTransform NewCell(int index)
        {
            var cell = Instantiate(cellTemplate, gridContent, false);
            cell.gameObject.SetActive(true);
            cell.name = "Cell" + index;
            int r = index / Mathf.Max(1, columns), c = index % Mathf.Max(1, columns);
            float x0 = -(columns - 1) * cellPitch.x / 2f;
            cell.anchoredPosition = new Vector2(x0 + c * cellPitch.x, -cellPitch.y / 2f - 6f - r * cellPitch.y);
            _cells.Add(cell.gameObject);
            int rows = Mathf.CeilToInt((index + 1) / (float)columns);
            gridContent.sizeDelta = new Vector2(gridContent.sizeDelta.x, rows * cellPitch.y + 12f);
            return cell;
        }

        private void Fill(RectTransform cell, string name, string sub, bool owned)
        {
            var n = Child<TextMeshProUGUI>(cell, "Name");
            if (n != null) n.text = name;
            var s = Child<TextMeshProUGUI>(cell, "Sub");
            if (s != null) { s.text = sub; s.color = owned ? new Color32(0x2E, 0x7A, 0x32, 255) : new Color32(120, 96, 70, 255); }
            var lockIcon = Child<Image>(cell, "Lock");
            if (lockIcon != null) lockIcon.gameObject.SetActive(!owned);
        }

        private static void OnTap(RectTransform cell, System.Action action)
        {
            var b = cell.GetComponent<Button>();
            if (b != null) b.onClick.AddListener(() => action());
        }

        private static void OpenShop(StoreScreen.Tab tab, string message)
        {
            if (Toast.Instance != null) Toast.Instance.Show(message);
            ScreenManager.Instance.Replace(ScreenId.Shop, tab);
        }

        private void SetProgress(int have, int total, string what)
        {
            if (progressLabel != null) progressLabel.text = have + " / " + total + " " + what;
            if (progressFill != null)
            {
                float pct = total > 0 ? Mathf.Clamp01(have / (float)total) : 0f;
                progressFill.anchorMin = Vector2.zero;
                progressFill.anchorMax = new Vector2(pct, 1f);
                progressFill.offsetMin = progressFill.offsetMax = Vector2.zero;
                progressFill.gameObject.SetActive(pct > 0.001f);
            }
        }

        private void Clear()
        {
            foreach (var go in _cells) if (go != null) Destroy(go);
            _cells.Clear();
        }

        private static T Child<T>(Transform root, string name) where T : Component
        {
            var t = root.Find(name);
            return t != null ? t.GetComponent<T>() : null;
        }
    }
}
