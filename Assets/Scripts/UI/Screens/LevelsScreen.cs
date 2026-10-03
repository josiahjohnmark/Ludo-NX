using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// LEVELS — the mission ladder. Reached through the vault doors from the bottom nav.
    ///
    /// Hero: the current level's badge, name, difficulty and "n / 5 missions" bar.
    /// Road: all ten level badges in a row — done ones lit and ticked, the current one idling,
    ///       locked ones dark. Tapping one shows its missions below.
    /// List: the five missions of the selected level with live progress from MissionService.
    ///
    /// The frame is baked by LudoNXLevelsBuilder; rows and road nodes are cloned from its
    /// templates here, so the table in MissionService is the only place levels are defined.
    /// </summary>
    public class LevelsScreen : UIScreen
    {
        [Header("Header")]
        public Button backButton;

        [Header("Hero")]
        public RectTransform heroBadgeRoot;
        public Image heroBadge;
        public RectTransform heroGlow;
        public TextMeshProUGUI heroLevel;
        public TextMeshProUGUI heroName;
        public TextMeshProUGUI heroDifficulty;
        public RectTransform heroFill;
        public TextMeshProUGUI heroProgress;
        public TextMeshProUGUI heroReward;
        public CanvasGroup levelUpBanner;
        public TextMeshProUGUI levelUpText;

        [Header("Road")]
        public ScrollRect roadScroll;
        public RectTransform roadContent;
        public RectTransform nodeTemplate;
        public float nodeSpacing = 78f;

        [Header("Missions")]
        public TextMeshProUGUI listTitle;
        public RectTransform listContent;
        public RectTransform rowTemplate;
        public float rowSpacing = 82f;

        [Header("Art")]
        public Sprite[] badges = new Sprite[10];
        public Sprite iconDice, iconBadge, iconStat;

        private readonly List<RectTransform> _nodes = new List<RectTransform>();
        private readonly List<RectTransform> _rows = new List<RectTransform>();
        private int _selected;
        private Coroutine _idle, _glowSpin;

        private static readonly Color Locked = new Color(0.18f, 0.19f, 0.23f, 1f);

        private void Awake()
        {
            if (backButton != null) backButton.onClick.AddListener(() => ScreenManager.Instance.Pop());
            if (nodeTemplate != null) nodeTemplate.gameObject.SetActive(false);
            if (rowTemplate != null) rowTemplate.gameObject.SetActive(false);
            if (levelUpBanner != null) levelUpBanner.alpha = 0f;
        }

        private void OnEnable() { MissionService.Changed += Refresh; }
        private void OnDisable() { MissionService.Changed -= Refresh; }

        public override void OnShow(object args)
        {
            var nav = ScreenManager.Instance != null ? ScreenManager.Instance.BottomNav : null;
            if (nav != null) nav.SetActiveTab(HomeNavBar.Tab.Levels);

            _selected = MissionService.SetIndex >= MissionService.Sets.Length
                ? MissionService.Sets.Length - 1
                : MissionService.SetIndex;
            BuildRoad();
            Refresh();
            StartCoroutine(Entrance(MissionService.TakePendingLevelUps()));
        }

        public override void OnHide()
        {
            StopAllCoroutines();
            _idle = _glowSpin = null;
        }

        // ======================================================================================
        // hero
        // ======================================================================================

        private void Refresh()
        {
            int cur = Mathf.Min(MissionService.SetIndex, MissionService.Sets.Length - 1);
            var set = MissionService.Sets[cur];
            bool champ = MissionService.IsChampion;

            if (heroBadge != null) heroBadge.sprite = Badge(cur);
            if (heroLevel != null) heroLevel.text = champ ? "NX CHAMPION" : "LEVEL " + set.level;
            if (heroName != null) heroName.text = champ ? "ALL 50 MISSIONS COMPLETE" : set.name;
            if (heroDifficulty != null) heroDifficulty.text = champ ? "MASTER" : set.difficulty;
            if (heroProgress != null) heroProgress.text = MissionService.DoneCount + " / 5 MISSIONS";
            if (heroFill != null) SetFill(heroFill, MissionService.LevelProgress01);
            if (heroReward != null)
            {
                heroReward.text = champ
                    ? "You have reached the top of the ladder."
                    : "Finish all 5 to reach LEVEL " + (set.level + 1 > MissionService.MaxLevel ? "MAX" : (set.level + 1).ToString())
                      + "  ·  +" + MissionService.LevelUpCoinsPerLevel * Mathf.Min(set.level + 1, MissionService.MaxLevel) + " coins";
            }

            RefreshNodes();
            ShowMissions(_selected);
        }

        private Sprite Badge(int setIndex)
        {
            if (badges == null || badges.Length == 0) return null;
            return badges[Mathf.Clamp(setIndex, 0, badges.Length - 1)];
        }

        private static void SetFill(RectTransform fill, float pct)
        {
            pct = Mathf.Clamp01(pct);
            fill.anchorMin = new Vector2(0f, 0f);
            fill.anchorMax = new Vector2(pct, 1f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            fill.gameObject.SetActive(pct > 0.001f);
        }

        // ======================================================================================
        // road
        // ======================================================================================

        private void BuildRoad()
        {
            if (nodeTemplate == null || roadContent == null || _nodes.Count > 0) return;
            int n = MissionService.Sets.Length;
            for (int i = 0; i < n; i++)
            {
                var node = Instantiate(nodeTemplate, roadContent, false);
                node.name = "Level" + (i + 1);
                node.gameObject.SetActive(true);
                node.anchoredPosition = new Vector2(nodeSpacing * 0.5f + 12f + i * nodeSpacing, 0f);
                int captured = i;
                var btn = node.GetComponent<Button>();
                if (btn != null) btn.onClick.AddListener(() => Select(captured));
                _nodes.Add(node);
            }
            roadContent.sizeDelta = new Vector2(nodeSpacing * n + 24f, roadContent.sizeDelta.y);
        }

        private void RefreshNodes()
        {
            int cur = MissionService.SetIndex;
            for (int i = 0; i < _nodes.Count; i++)
            {
                var node = _nodes[i];
                bool done = i < cur, current = i == cur, locked = i > cur;

                var badge = Find<Image>(node, "Badge");
                if (badge != null) { badge.sprite = Badge(i); badge.color = locked ? Locked : Color.white; }
                SetActive(node, "Tick", done);
                SetActive(node, "Lock", locked);
                SetActive(node, "CurrentRing", current);
                SetActive(node, "SelectedMark", i == _selected);
                SetActive(node, "Link", i < _nodes.Count - 1);
                var label = Find<TextMeshProUGUI>(node, "Number");
                if (label != null)
                {
                    label.text = (i + 1).ToString();
                    label.color = locked ? new Color(1f, 1f, 1f, 0.45f) : Color.white;
                }
                var track = Find<Image>(node, "Link");
                if (track != null) track.color = i < cur ? new Color32(232, 185, 38, 255) : new Color(1f, 1f, 1f, 0.18f);
            }
        }

        private void Select(int i)
        {
            if (i == _selected) return;
            _selected = i;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButton();
            RefreshNodes();
            ShowMissions(i);
            StartCoroutine(StaggerRows());
        }

        private void ScrollRoadTo(int i, bool instant)
        {
            if (roadScroll == null || roadContent == null || _nodes.Count == 0) return;
            float viewW = ((RectTransform)roadScroll.transform).rect.width;
            float contentW = roadContent.sizeDelta.x;
            if (contentW <= viewW) return;
            float x = _nodes[Mathf.Clamp(i, 0, _nodes.Count - 1)].anchoredPosition.x;
            float target = Mathf.Clamp01((x - viewW * 0.5f) / (contentW - viewW));
            if (instant) roadScroll.horizontalNormalizedPosition = target;
            else StartCoroutine(Ease(roadScroll.horizontalNormalizedPosition, target, 0.6f,
                v => roadScroll.horizontalNormalizedPosition = v));
        }

        // ======================================================================================
        // missions list
        // ======================================================================================

        private void ShowMissions(int setIndex)
        {
            if (rowTemplate == null || listContent == null) return;
            var set = MissionService.Sets[setIndex];
            bool locked = MissionService.IsLocked(setIndex);

            if (listTitle != null)
                listTitle.text = "LEVEL " + set.level + "  ·  " + set.name + (locked ? "  ·  LOCKED" : "");

            while (_rows.Count < set.missions.Length)
            {
                var row = Instantiate(rowTemplate, listContent, false);
                row.gameObject.SetActive(true);
                row.name = "Mission" + (_rows.Count + 1);
                row.anchoredPosition = new Vector2(0f, -(rowSpacing * 0.5f) - _rows.Count * rowSpacing);
                _rows.Add(row);
            }

            for (int i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];
                var m = set.missions[i];
                bool done = MissionService.IsDone(setIndex, i);
                int prog = MissionService.Progress(setIndex, i);
                int target = m.Target;

                var title = Find<TextMeshProUGUI>(row, "Title");
                if (title != null) title.text = m.title.ToUpperInvariant();
                var desc = Find<TextMeshProUGUI>(row, "Desc");
                if (desc != null) desc.text = m.desc;
                var count = Find<TextMeshProUGUI>(row, "Count");
                if (count != null) count.text = done ? "DONE" : locked ? "LOCKED" : prog + " / " + target;
                var fill = Find<RectTransform>(row, "Fill");
                if (fill != null) SetFill(fill, locked ? 0f : prog / (float)target);
                var icon = Find<Image>(row, "Icon");
                if (icon != null) icon.sprite = IconFor(m);
                SetActive(row, "Tick", done);
                SetActive(row, "LockShade", locked);
                var card = Find<Image>(row, "Card");
                if (card != null) card.color = done ? new Color(1f, 0.96f, 0.82f, 1f) : Color.white;
            }
        }

        private Sprite IconFor(MissionService.Mission m)
        {
            string key = m.reqs[0].key;
            if (key == MissionService.K.Rolls || key == MissionService.K.Sixes || key == MissionService.K.Spaces
                || key == MissionService.K.Out || key == MissionService.K.Column || key == MissionService.K.Moved
                || key == MissionService.K.MaxActive)
                return iconDice;
            if (key == MissionService.K.Wins || key == MissionService.K.Won) return iconBadge;
            return iconStat;
        }

        // ======================================================================================
        // motion — tokens.json motion.*
        // ======================================================================================

        private IEnumerator Entrance(int levelUps)
        {
            yield return null;   // let layout settle before measuring the road
            ScrollRoadTo(_selected, true);

            // countUp: the bar fills from empty, 600–1200 ms outCubic.
            if (heroFill != null)
            {
                float target = MissionService.LevelProgress01;
                StartCoroutine(Ease(0f, target, 0.8f, v => SetFill(heroFill, v)));
            }
            StartCoroutine(StaggerRows());

            if (levelUps > 0) yield return LevelUpCelebration();

            // playIdle: one idling element per screen — the current level's node.
            int cur = Mathf.Min(MissionService.SetIndex, _nodes.Count - 1);
            if (cur >= 0 && cur < _nodes.Count) _idle = StartCoroutine(Idle(_nodes[cur]));
            if (heroGlow != null) _glowSpin = StartCoroutine(Spin(heroGlow));
        }

        private IEnumerator StaggerRows()
        {
            // listStagger: 30 ms per row, 12 pt slide up, 180 ms fade.
            for (int i = 0; i < _rows.Count; i++)
            {
                var cg = _rows[i].GetComponent<CanvasGroup>();
                if (cg != null) cg.alpha = 0f;
            }
            for (int i = 0; i < _rows.Count; i++)
            {
                StartCoroutine(RowIn(_rows[i], -(rowSpacing * 0.5f) - i * rowSpacing));
                yield return new WaitForSecondsRealtime(0.03f);
            }
        }

        private IEnumerator RowIn(RectTransform row, float y)
        {
            var cg = row.GetComponent<CanvasGroup>();
            yield return Ease(0f, 1f, 0.18f, t =>
            {
                row.anchoredPosition = new Vector2(0f, y - 12f * (1f - t));
                if (cg != null) cg.alpha = t;
            });
        }

        private IEnumerator LevelUpCelebration()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayVictory();
            if (levelUpText != null)
                levelUpText.text = MissionService.IsChampion ? "NX CHAMPION!" : "LEVEL " + MissionService.Level + "!";

            if (heroBadgeRoot != null)
            {
                yield return Ease(0f, 1f, 0.45f, t =>
                {
                    // popupOpen scale curve 0.86 → 1.04 → 1.0, stretched for a hero moment.
                    float s = t < 0.6f ? Mathf.Lerp(0.3f, 1.22f, t / 0.6f) : Mathf.Lerp(1.22f, 1f, (t - 0.6f) / 0.4f);
                    heroBadgeRoot.localScale = new Vector3(s, s, 1f);
                });
            }
            if (levelUpBanner != null)
            {
                var rt = (RectTransform)levelUpBanner.transform;
                yield return Ease(0f, 1f, 0.26f, t =>
                {
                    levelUpBanner.alpha = t;
                    float s = Mathf.Lerp(0.86f, 1f, t) + Mathf.Sin(t * Mathf.PI) * 0.06f;
                    rt.localScale = new Vector3(s, s, 1f);
                });
                yield return new WaitForSecondsRealtime(1.6f);
                yield return Ease(1f, 0f, 0.16f, t => levelUpBanner.alpha = t);
            }
        }

        private static IEnumerator Idle(RectTransform node)
        {
            float t0 = Time.unscaledTime;
            while (node != null)
            {
                float k = (Mathf.Sin((Time.unscaledTime - t0) / 1.6f * Mathf.PI * 2f) + 1f) * 0.5f;
                float s = Mathf.Lerp(1f, 1.03f, k);
                node.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
        }

        private static IEnumerator Spin(RectTransform rt)
        {
            while (rt != null)
            {
                rt.Rotate(0f, 0f, -12f * Time.unscaledDeltaTime);   // victory.raysDegPerSec
                yield return null;
            }
        }

        private static IEnumerator Ease(float from, float to, float seconds, System.Action<float> set)
        {
            float t0 = Time.unscaledTime;
            while (true)
            {
                float t = Mathf.Clamp01((Time.unscaledTime - t0) / Mathf.Max(0.001f, seconds));
                float e = 1f - Mathf.Pow(1f - t, 3f);
                set(Mathf.LerpUnclamped(from, to, e));
                if (t >= 1f) yield break;
                yield return null;
            }
        }

        // ======================================================================================

        private static T Find<T>(Transform root, string name) where T : Component
        {
            var t = FindDeep(root, name);
            return t != null ? t.GetComponent<T>() : null;
        }

        private static void SetActive(Transform root, string name, bool on)
        {
            var t = FindDeep(root, name);
            if (t != null) t.gameObject.SetActive(on);
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var r = FindDeep(root.GetChild(i), name);
                if (r != null) return r;
            }
            return null;
        }
    }
}
