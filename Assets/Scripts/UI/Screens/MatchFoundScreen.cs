using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// The moment between tapping FIND OPPONENT and the board appearing: searching, found, then a
    /// countdown. Three states on one screen, because they are one continuous beat and a player
    /// should never wonder whether the game is stuck.
    ///
    /// The search takes a short, slightly random time. That is not theatre for its own sake — a
    /// result that lands instantly reads as fake, and the pause is where the opponent's name and
    /// standing get a chance to be read. It is capped low enough that nobody waits on a loading
    /// screen for the sake of realism.
    ///
    /// The opponent is a bot from <see cref="BotRoster"/> while there is no server. Nothing here
    /// claims otherwise: the screen says "Opponent found", never "online now". When matchmaking
    /// becomes real, only <see cref="FindOpponent"/> changes.
    ///
    /// Countdown starts at 3. Five is long enough for a player to look away, and the reference
    /// mock counts from 3.
    /// </summary>
    public class MatchFoundScreen : UIScreen
    {
        [Header("Split background")]
        public Image splitTop;
        public Image splitBottom;

        [Header("You")]
        public TextMeshProUGUI yourName;
        public Image yourPortrait;
        public TextMeshProUGUI yourRating;
        public Image[] yourPawns;

        [Header("Opponent")]
        public CanvasGroup opponentGroup;
        public TextMeshProUGUI opponentName;
        public Image opponentPortrait;
        public TextMeshProUGUI opponentRating;
        public TextMeshProUGUI opponentStyle;
        public Image[] opponentPawns;

        [Header("Centre")]
        public CanvasGroup searchingGroup;
        public TextMeshProUGUI searchingLabel;
        public RectTransform spinner;
        public CanvasGroup countdownGroup;
        public TextMeshProUGUI countdownNumber;
        public TextMeshProUGUI stakeLabel;
        public Image vsBadge;

        [Header("Cancel")]
        public Button cancelButton;
        public TextMeshProUGUI cancelLabel;

        [Header("Art")]
        public Sprite[] avatars;
        public Sprite[] pawnSprites;        // red, green, yellow, blue
        public Sprite defaultPortrait;

        [Header("Feel")]
        public float searchMin = 1.1f;
        public float searchMax = 2.3f;
        public float foundHold = 1.0f;
        public int countFrom = 3;

        /// <summary>
        /// Deeper than the board's house colours on purpose. At full saturation the two halves
        /// fight each other and the cream type loses contrast; dropping the brightness lets the
        /// names, avatars and countdown sit on top without an outline.
        /// </summary>
        private static readonly Color[] HouseColours =
        {
            new Color32(166, 42, 34, 255),      // red
            new Color32(40, 112, 58, 255),      // green
            new Color32(176, 126, 26, 255),     // yellow
            new Color32(30, 88, 158, 255),      // blue
        };

        private Coroutine _flow;
        private BotRoster.Bot _opponent;
        private int _multiplier = 1;

        private void Awake()
        {
            if (cancelButton != null) cancelButton.onClick.AddListener(OnCancel);
        }

        public override void OnShow(object args)
        {
            if (args is int m) _multiplier = Mathf.Max(1, m);
            else if (LudoNXSession.Instance != null) _multiplier = Mathf.Max(1, LudoNXSession.Instance.pendingMultiplier);

            PaintYou();
            PaintHouses();
            SetOpponentVisible(false);

            if (countdownGroup != null) { countdownGroup.alpha = 0f; }
            if (searchingGroup != null) { searchingGroup.alpha = 1f; }
            if (cancelButton != null) cancelButton.gameObject.SetActive(true);
            if (searchingLabel != null) searchingLabel.text = "Searching for an opponent";
            if (stakeLabel != null)
            {
                int win = MultiplierService.PointsOnWin(20, _multiplier);
                stakeLabel.text = $"x{_multiplier}   ·   Win +{win:n0} NX";
            }

            _flow = StartCoroutine(Flow());
        }

        public override void OnHide()
        {
            if (_flow != null) { StopCoroutine(_flow); _flow = null; }
        }

        /// <summary>Back during the search is a cancel; once the countdown starts it is too late.</summary>
        public override bool OnBack()
        {
            if (countdownGroup != null && countdownGroup.alpha > 0.5f) return true;
            OnCancel();
            return true;
        }

        // ==================================================================================
        // flow
        // ==================================================================================

        private IEnumerator Flow()
        {
            float wait = Random.Range(searchMin, searchMax);
            float t = 0f;
            int dots = 0;
            while (t < wait)
            {
                t += Time.unscaledDeltaTime;
                if (spinner != null) spinner.Rotate(0f, 0f, -220f * Time.unscaledDeltaTime);

                // The dots move so a slow search still looks alive.
                int next = Mathf.FloorToInt(t * 3f) % 4;
                if (next != dots && searchingLabel != null)
                {
                    dots = next;
                    searchingLabel.text = "Searching for an opponent" + new string('.', dots);
                }
                yield return null;
            }

            FindOpponent();
            SetOpponentVisible(true);
            if (searchingLabel != null) searchingLabel.text = "Opponent found";
            yield return StartCoroutine(PopIn(opponentGroup));
            yield return new WaitForSecondsRealtime(foundHold);

            if (cancelButton != null) cancelButton.gameObject.SetActive(false);
            if (searchingGroup != null) searchingGroup.alpha = 0f;
            if (countdownGroup != null) countdownGroup.alpha = 1f;

            for (int n = countFrom; n >= 1; n--)
            {
                if (countdownNumber != null) countdownNumber.text = n.ToString();
                yield return StartCoroutine(PopNumber());
            }

            if (countdownNumber != null) countdownNumber.text = "GO";
            yield return new WaitForSecondsRealtime(0.35f);

            // Replace rather than push: nobody should land back on a finished countdown.
            ScreenManager.Instance.Replace(ScreenId.MatchIntro);
        }

        private void FindOpponent()
        {
            int rating = LudoNXSession.Instance != null ? LudoNXSession.Instance.NXPoints : 0;
            _opponent = BotRoster.Pick(rating);

            if (opponentName != null) opponentName.text = _opponent.name.ToUpperInvariant();
            if (opponentRating != null) opponentRating.text = $"{_opponent.rating:n0} NX";
            if (opponentStyle != null) opponentStyle.text = _opponent.StyleLabel;
            if (opponentPortrait != null && avatars != null && avatars.Length > 0)
                opponentPortrait.sprite = avatars[Mathf.Abs(_opponent.avatar) % avatars.Length];

            if (LudoNXSession.Instance != null)
                LudoNXSession.Instance.pendingOpponentName = _opponent.name;
        }

        private void PaintYou()
        {
            var s = LudoNXSession.Instance;
            if (yourName != null) yourName.text = (s != null ? s.PlayerName : "YOU").ToUpperInvariant();
            if (yourRating != null) yourRating.text = $"{(s != null ? s.NXPoints : 0):n0} NX";
            if (yourPortrait != null && defaultPortrait != null) yourPortrait.sprite = defaultPortrait;
        }

        /// <summary>
        /// Each side owns a diagonal, and the background is tinted with those houses so the split
        /// tells the player which colours they are about to play.
        /// </summary>
        private void PaintHouses()
        {
            var s = LudoNXSession.Instance;
            int mine = s != null ? (int)s.pendingHumanColor : 0;
            mine = Mathf.Clamp(mine, 0, 3);
            int partner = (mine + 2) % 4;
            int foeA = (mine + 1) % 4;
            int foeB = (mine + 3) % 4;

            if (splitTop != null) splitTop.color = HouseColours[mine];
            if (splitBottom != null) splitBottom.color = HouseColours[foeA];

            Assign(yourPawns, mine, partner);
            Assign(opponentPawns, foeA, foeB);
        }

        private void Assign(Image[] slots, int first, int second)
        {
            if (slots == null || pawnSprites == null) return;
            if (slots.Length > 0 && slots[0] != null && first < pawnSprites.Length) slots[0].sprite = pawnSprites[first];
            if (slots.Length > 1 && slots[1] != null && second < pawnSprites.Length) slots[1].sprite = pawnSprites[second];
        }

        private void SetOpponentVisible(bool visible)
        {
            if (opponentGroup != null) opponentGroup.alpha = visible ? 1f : 0f;
            if (opponentName != null && !visible) opponentName.text = "";
            if (opponentRating != null && !visible) opponentRating.text = "";
            if (opponentStyle != null && !visible) opponentStyle.text = "";
        }

        private IEnumerator PopIn(CanvasGroup group)
        {
            if (group == null) yield break;
            var rt = (RectTransform)group.transform;
            float t = 0f;
            while (t < 0.28f)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / 0.28f);
                group.alpha = k;
                float s = 0.88f + 0.12f * (1f - (1f - k) * (1f - k));
                rt.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            group.alpha = 1f;
            rt.localScale = Vector3.one;
        }

        private IEnumerator PopNumber()
        {
            var rt = countdownNumber != null ? (RectTransform)countdownNumber.transform : null;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime;
                if (rt != null)
                {
                    // A quick punch at the top of each second, then settle.
                    float s = t < 0.22f ? Mathf.Lerp(1.5f, 1f, t / 0.22f) : 1f;
                    rt.localScale = new Vector3(s, s, 1f);
                }
                yield return null;
            }
        }

        private void OnCancel()
        {
            if (_flow != null) { StopCoroutine(_flow); _flow = null; }
            ScreenManager.Instance.Pop();
        }
    }
}
