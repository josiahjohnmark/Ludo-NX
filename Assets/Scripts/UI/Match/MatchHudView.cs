using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LudoNX.UI
{
    /// <summary>
    /// The in-match HUD: four corner player cards, the turn state, the stake plaque, the roll slab,
    /// dynamic house turn tinting over the wood wallpaper, and the bottom action bar (emoji, RULES, timer, message).
    /// </summary>
    public class MatchHudView : MonoBehaviour
    {
        [Header("Top bar")]
        public Button pauseButton;
        public GameObject stakePlaque;
        public TextMeshProUGUI stakeLabel;
        public Button muteButton;
        public Image muteIcon;
        public Sprite soundOnIcon;
        public Sprite soundOffIcon;

        [Header("Action Bar (Bottom)")]
        public Button emojiButton;
        public Button rulesButton;
        public Button messageButton;
        public RectTransform turnTimerRoot;
        public TextMeshProUGUI turnTimerLabel;
        public CanvasGroup turnTimerGroup;

        [Header("House Turn Tint (Layer 2) — Top, Bottom, Left, Right strips framing the board")]
        public Image[] houseTintStrips;

        [Tooltip("The live board sprite. Found by name so the tint strips can hug its actual on-screen rect and never paint over it (MATCH_HUD_V2.md §1).")]
        public string boardObjectName = "Ludo board game in vibrant colours_0";

        [Header("Player cards, in board order: red, green, yellow, blue")]
        public MatchPlayerCard[] cards;

        [Header("Roll Area")]
        public TextMeshProUGUI turnLabel;
        public Button rollButton;
        public Image rollBackground;
        public TextMeshProUGUI rollLabel;
        public TextMeshProUGUI rollSubLabel;
        public CanvasGroup rollGroup;

        [Header("Art")]
        public Sprite rollEnabled;
        public Sprite rollDisabled;

        [Header("Feel")]
        [Tooltip("Seconds a player gets before the turn is nudged along. Zero disables the timer.")]
        public float turnSeconds = 30f;
        public float pulseSpeed = 2.2f;

        private static readonly GameManager.PlayerColor[] Order =
        {
            GameManager.PlayerColor.RED,
            GameManager.PlayerColor.GREEN,
            GameManager.PlayerColor.YELLOW,
            GameManager.PlayerColor.BLUE,
        };

        private TokenMovement[] _tokens;
        private float _turnStarted;
        private GameManager.PlayerColor _lastTurn = (GameManager.PlayerColor)(-1);
        private string _lastTurnLabel;
        private bool _autoRolled;

        private Color _currentTint = Color.clear;
        private Color _targetTint = Color.clear;
        private const float TintBlendDuration = 0.32f; // 320 ms per MATCH_HUD_V2.md §2

        private SpriteRenderer _board;
        private Camera _boardCam;

        private void Awake()
        {
            EnforceSnugBottomBar();
        }

        private void OnEnable()
        {
            EnforceSnugBottomBar();
            _tokens = Object.FindObjectsByType<TokenMovement>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            _turnStarted = Time.unscaledTime;
            
            if (pauseButton != null)
            {
                pauseButton.onClick.RemoveAllListeners();
                pauseButton.onClick.AddListener(OnPause);
            }
            if (rollButton != null)
            {
                rollButton.onClick.RemoveAllListeners();
                rollButton.onClick.AddListener(OnRoll);
            }
            if (rulesButton != null)
            {
                rulesButton.onClick.RemoveAllListeners();
                rulesButton.onClick.AddListener(OnRules);
            }
            if (muteButton != null)
            {
                muteButton.onClick.RemoveAllListeners();
                muteButton.onClick.AddListener(OnToggleMute);
                UpdateMuteVisuals();
            }
            if (emojiButton != null)
            {
                emojiButton.onClick.RemoveAllListeners();
                emojiButton.onClick.AddListener(OnEmoji);
            }
            if (messageButton != null)
            {
                messageButton.onClick.RemoveAllListeners();
                messageButton.onClick.AddListener(OnMessage);
            }

            Bind();
        }

        /// <summary>
        /// Enforces the pixel-perfect flush alignment of the 3D Roll button into the tan footer
        /// arch socket cutout directly atop the timer pill with zero gap, matching Figma.
        /// Runs at runtime so even un-rebuilt prefabs display the perfect layout.
        /// </summary>
        public void EnforceSnugBottomBar()
        {
            var band = transform.Find("BottomBand") as RectTransform;
            if (band != null)
            {
                band.anchorMin = new Vector2(0f, 0f);
                band.anchorMax = new Vector2(1f, 0f);
                band.pivot = new Vector2(0.5f, 0f);
                band.sizeDelta = new Vector2(0f, 220f);
            }

            var footer = transform.Find("BottomBand/FooterPanel") as RectTransform;
            if (footer != null)
            {
                footer.anchorMin = new Vector2(0f, 0f);
                footer.anchorMax = new Vector2(1f, 0f);
                footer.pivot = new Vector2(0.5f, 0f);
                footer.sizeDelta = new Vector2(0f, 150f);
                footer.anchoredPosition = Vector2.zero;
            }

            if (rollButton != null)
            {
                rollButton.transition = Selectable.Transition.None;
                var rt = rollButton.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(116f, 68f);
                    rt.anchoredPosition = new Vector2(0f, 112f);
                }
            }

            if (turnTimerRoot != null)
            {
                turnTimerRoot.anchorMin = turnTimerRoot.anchorMax = new Vector2(0.5f, 0f);
                turnTimerRoot.pivot = new Vector2(0.5f, 0.5f);
                turnTimerRoot.sizeDelta = new Vector2(92f, 34f);
                turnTimerRoot.anchoredPosition = new Vector2(0f, 62f);
            }

            if (emojiButton != null)
            {
                var rt = emojiButton.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(50f, 50f);
                    rt.anchoredPosition = new Vector2(54f, 62f);
                }
            }

            if (messageButton != null)
            {
                var rt = messageButton.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(50f, 50f);
                    rt.anchoredPosition = new Vector2(-54f, 62f);
                }
            }

            if (rollLabel != null)
            {
                rollLabel.text = "ROLL";
                rollLabel.characterSpacing = 1.5f;
                rollLabel.outlineWidth = 0.25f;
                rollLabel.outlineColor = new Color32(110, 10, 10, 230);
                rollLabel.rectTransform.anchorMin = new Vector2(0f, 0.32f);
                rollLabel.rectTransform.anchorMax = new Vector2(1f, 0.94f);
                rollLabel.rectTransform.offsetMin = Vector2.zero;
                rollLabel.rectTransform.offsetMax = Vector2.zero;
            }

            if (rollSubLabel != null)
            {
                rollSubLabel.text = "TAP TO ROLL";
                rollSubLabel.fontSize = 8.5f;
                rollSubLabel.color = new Color32(235, 140, 150, 255);
                rollSubLabel.rectTransform.anchorMin = new Vector2(0f, 0.12f);
                rollSubLabel.rectTransform.anchorMax = new Vector2(1f, 0.36f);
                rollSubLabel.rectTransform.offsetMin = Vector2.zero;
                rollSubLabel.rectTransform.offsetMax = Vector2.zero;
            }
            else if (rollButton != null)
            {
                var existing = rollButton.transform.Find("RollSubLabel");
                if (existing != null)
                {
                    rollSubLabel = existing.GetComponent<TextMeshProUGUI>();
                }
                else
                {
                    var subGo = new GameObject("RollSubLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
                    subGo.transform.SetParent(rollButton.transform, false);
                    rollSubLabel = subGo.GetComponent<TextMeshProUGUI>();
                    if (rollLabel != null) rollSubLabel.font = rollLabel.font;
                }

                if (rollSubLabel != null)
                {
                    rollSubLabel.text = "TAP TO ROLL";
                    rollSubLabel.fontSize = 8.5f;
                    rollSubLabel.color = new Color32(235, 140, 150, 255);
                    rollSubLabel.alignment = TextAlignmentOptions.Center;
                    rollSubLabel.rectTransform.anchorMin = new Vector2(0f, 0.12f);
                    rollSubLabel.rectTransform.anchorMax = new Vector2(1f, 0.36f);
                    rollSubLabel.rectTransform.offsetMin = Vector2.zero;
                    rollSubLabel.rectTransform.offsetMax = Vector2.zero;
                }
            }
        }

        // ==================================================================================
        // binding
        // ==================================================================================

        /// <summary>Fills in the four cards once, at the start of a match.</summary>
        public void Bind()
        {
            var gm = GameManager.Instance;
            var session = LudoNXSession.Instance;
            if (gm == null || cards == null) return;

            bool isTwoPlayerFourHouse = session != null && (session.pendingTeamPlay || (session.pendingMode == LudoNXSession.Mode.QuickMatch && session.pendingPlayerCount == 2));

            if (isTwoPlayerFourHouse)
            {
                // In 2-player 4-house mode, only 2 cards: Card 0 (Red+Yellow) and Card 1 (Green+Blue)
                var p1Card = cards.Length > 0 ? cards[0] : null;
                var p2Card = cards.Length > 1 ? cards[1] : null;

                if (p1Card != null)
                {
                    p1Card.gameObject.SetActive(true);
                    p1Card.ApplyHouse(GameManager.PlayerColor.RED, false);
                    p1Card.onLongPress = null;
                    if (p1Card.nameLabel != null)
                        p1Card.nameLabel.text = string.IsNullOrEmpty(session?.PlayerName) ? "YOU (RED/YELLOW)" : $"{session.PlayerName} (RED/YLW)";
                    if (p1Card.levelLabel != null) p1Card.levelLabel.text = "LVL " + (session != null ? session.Level : 1);
                    if (p1Card.nxPointsLabel != null) p1Card.nxPointsLabel.text = (session != null ? session.NXPoints : 0).ToString("N0");
                    if (p1Card.winPercentLabel != null) p1Card.winPercentLabel.text = (session != null && session.MatchesPlayed > 0 ? Mathf.RoundToInt(session.WinRate * 100f) : 0) + "% WIN";
                    p1Card.SetTokensHome(0);
                }

                if (p2Card != null)
                {
                    bool isBot = gm.aiPlaysOtherColors;
                    p2Card.gameObject.SetActive(true);
                    p2Card.ApplyHouse(GameManager.PlayerColor.GREEN, isBot);
                    p2Card.onLongPress = ToggleMute;
                    p2Card.SetMuted(_mutedHouses.Contains(GameManager.PlayerColor.GREEN));
                    string oppName = isBot ? "BOT (GREEN/BLUE)" : (string.IsNullOrEmpty(session?.pendingOpponentName) ? "OPPONENT (GRN/BLU)" : $"{session.pendingOpponentName} (GRN/BLU)");
                    if (p2Card.nameLabel != null) p2Card.nameLabel.text = oppName;
                    if (p2Card.levelLabel != null) p2Card.levelLabel.text = "LVL 1";
                    if (p2Card.nxPointsLabel != null) p2Card.nxPointsLabel.text = "0";
                    if (p2Card.winPercentLabel != null) p2Card.winPercentLabel.text = "0% WIN";
                    p2Card.SetTokensHome(0);
                }

                for (int i = 2; i < cards.Length; i++)
                {
                    if (cards[i] != null) cards[i].gameObject.SetActive(false);
                }

                RulesModal.CheckFirstMatchAutoOpen();
                var f = Camera.main != null ? Camera.main.GetComponent<BoardCameraFramer>() : null;
                if (f != null) f.Frame();
                return;
            }

            var names = NamesByHouse(gm, session);
            int human = System.Array.IndexOf(Order, gm.humanColor);

            for (int i = 0; i < cards.Length && i < Order.Length; i++)
            {
                var card = cards[i];
                if (card == null) continue;

                bool playing = gm.activePlayers != null && gm.activePlayers.Contains(Order[i]);
                card.gameObject.SetActive(playing);
                if (!playing) continue;

                bool isBot = (i != human && gm.aiPlaysOtherColors);
                card.ApplyHouse(Order[i], isBot);
                card.onLongPress = ToggleMute;
                card.SetMuted(_mutedHouses.Contains(Order[i]));
                if (card.nameLabel != null) card.nameLabel.text = names[i];
                if (card.levelLabel != null) card.levelLabel.text = "LVL " + (i == human ? (session != null ? session.Level : 1) : 1);
                if (card.nxPointsLabel != null) card.nxPointsLabel.text = (i == human ? (session != null ? session.NXPoints : 0) : 0).ToString("N0");
                if (card.winPercentLabel != null) card.winPercentLabel.text = (i == human && session != null && session.MatchesPlayed > 0 ? Mathf.RoundToInt(session.WinRate * 100f) : 0) + "% WIN";
                card.SetTokensHome(0);
            }

            RulesModal.CheckFirstMatchAutoOpen();

            var framer = Camera.main != null ? Camera.main.GetComponent<BoardCameraFramer>() : null;
            if (framer != null) framer.Frame();
        }

        private string[] NamesByHouse(GameManager gm, LudoNXSession session)
        {
            var names = new string[4];
            for (int i = 0; i < 4; i++) names[i] = Order[i].ToString();
            if (session == null) return names;

            int human = System.Array.IndexOf(Order, gm.humanColor);
            if (human >= 0) names[human] = string.IsNullOrEmpty(session.PlayerName) ? "YOU" : session.PlayerName;

            var seats = session.pendingSeatNames;
            if (seats == null || seats.Count == 0) return names;

            int seat = 0;
            for (int i = 0; i < 4 && seat < seats.Count; i++)
            {
                if (i == human) continue;
                if (gm.activePlayers != null && !gm.activePlayers.Contains(Order[i])) continue;
                if (!string.IsNullOrEmpty(seats[seat])) names[i] = seats[seat];
                seat++;
            }
            return names;
        }

        // ==================================================================================
        // per-frame state
        // ==================================================================================

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            if (gm.currentTurnColor != _lastTurn)
            {
                _lastTurn = gm.currentTurnColor;
                _turnStarted = Time.unscaledTime;
                _autoRolled = false;
                UpdateTurnTintTarget(gm);
            }

            AnimateTurnTint();
            UpdateTintHole();
            PaintCards(gm);
            PaintTurn(gm);
            PaintRoll(gm);
            PaintTimer(gm);

            // Pass and Play hides the message button entirely (match-message-tray.json) — every
            // seat is a person on this device, and canned phrases read as talking to yourself.
            if (messageButton != null) messageButton.gameObject.SetActive(gm.aiPlaysOtherColors);
        }

        private void UpdateTurnTintTarget(GameManager gm)
        {
            bool inMatch = gm.activePlayers != null && gm.activePlayers.Count > 0;
            if (!inMatch)
            {
                _targetTint = Color.clear;
                return;
            }

            // Cinematic ambient color grading (~10% opacity overlay, framing the board, darker richer tones):
            _targetTint = gm.currentTurnColor switch
            {
                GameManager.PlayerColor.RED => new Color(0.55f, 0.08f, 0.08f, 0.10f),
                GameManager.PlayerColor.GREEN => new Color(0.08f, 0.45f, 0.15f, 0.10f),
                GameManager.PlayerColor.YELLOW => new Color(0.65f, 0.45f, 0.05f, 0.10f),
                GameManager.PlayerColor.BLUE => new Color(0.08f, 0.30f, 0.60f, 0.10f),
                _ => Color.clear
            };
        }

        private void AnimateTurnTint()
        {
            if (houseTintStrips == null) return;
            _currentTint = Color.Lerp(_currentTint, _targetTint, Time.unscaledDeltaTime / TintBlendDuration);
            foreach (var strip in houseTintStrips)
                if (strip != null) strip.color = _currentTint;
        }

        /// <summary>
        /// Resizes the four tint strips (top/bottom/left/right) to frame the board's actual
        /// on-screen rectangle, so the house tint always surrounds the board instead of painting
        /// over it — MATCH_HUD_V2.md §1: "the tint never covers the board - the board's four
        /// colours stay pure." The strips are Screen Space Overlay UI, which always draws above
        /// the board's world camera regardless of sorting order, so the hole has to be real
        /// geometry, not a lower sort index.
        /// </summary>
        public void UpdateTintHole()
        {
            if (houseTintStrips == null || houseTintStrips.Length < 4) return;

            if (_board == null && !string.IsNullOrEmpty(boardObjectName))
            {
                var go = GameObject.Find(boardObjectName);
                if (go != null) _board = go.GetComponent<SpriteRenderer>();
            }
            if (_boardCam == null) _boardCam = Camera.main;

            var canvasRt = (RectTransform)transform;
            float halfW = canvasRt.rect.width * 0.5f;
            float halfH = canvasRt.rect.height * 0.5f;
            if (halfW <= 0f || halfH <= 0f) return;

            float bLeft, bRight, bTop, bBottom;
            if (_board != null && _boardCam != null)
            {
                Bounds b = _board.bounds;
                Vector2 lo = WorldToCanvasPoint(b.min, canvasRt);
                Vector2 hi = WorldToCanvasPoint(b.max, canvasRt);
                bLeft = Mathf.Min(lo.x, hi.x);
                bRight = Mathf.Max(lo.x, hi.x);
                bBottom = Mathf.Min(lo.y, hi.y);
                bTop = Mathf.Max(lo.y, hi.y);
            }
            else
            {
                // No board found (e.g. an isolated HUD preview) — fall back to a centred square so
                // the strips still demonstrate a hole instead of collapsing to a full-bleed tint.
                float side = Mathf.Min(halfW, halfH) * 1.6f;
                bLeft = -side * 0.5f; bRight = side * 0.5f;
                bBottom = -side * 0.5f; bTop = side * 0.5f;
            }

            // Stop the bottom house tint strip strictly above the bottom action band (y = -halfH + 220f)
            // so the house turn colors never tint or overlay the Roll button and controls.
            float bottomBandTop = -halfH + 220f;
            float stripBottomY = Mathf.Max(bottomBandTop, -halfH);
            float stripTopY = Mathf.Max(stripBottomY, bBottom);

            PlaceStrip(houseTintStrips[0], -halfW, halfW, bTop, halfH);             // Top
            PlaceStrip(houseTintStrips[1], -halfW, halfW, stripBottomY, stripTopY); // Bottom (stopped above bottom bar)
            PlaceStrip(houseTintStrips[2], -halfW, bLeft, bBottom, bTop);           // Left
            PlaceStrip(houseTintStrips[3], bRight, halfW, bBottom, bTop);           // Right
        }

        private static void PlaceStrip(Image img, float xMin, float xMax, float yMin, float yMax)
        {
            if (img == null) return;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(Mathf.Max(0f, xMax - xMin), Mathf.Max(0f, yMax - yMin));
            rt.anchoredPosition = new Vector2((xMin + xMax) * 0.5f, (yMin + yMax) * 0.5f);
        }

        /// <summary>World point to canvas-local coordinates, origin at the canvas centre. Mirrors
        /// MatchHudLayout.WorldToCanvas — same technique, kept local here so this view has no
        /// dependency on that (scene-only, currently retired) layout driver.</summary>
        private Vector2 WorldToCanvasPoint(Vector3 world, RectTransform canvasRt)
        {
            Vector3 screen = _boardCam.WorldToScreenPoint(world);
            var canvas = canvasRt.GetComponentInParent<Canvas>();
            Camera uiCam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screen, uiCam, out Vector2 local);
            return local;
        }

        private void PaintCards(GameManager gm)
        {
            if (cards == null) return;
            var session = LudoNXSession.Instance;
            bool isTwoPlayerFourHouse = session != null && (session.pendingTeamPlay || (session.pendingMode == LudoNXSession.Mode.QuickMatch && session.pendingPlayerCount == 2));

            if (isTwoPlayerFourHouse)
            {
                if (cards.Length > 0 && cards[0] != null && cards[0].gameObject.activeSelf)
                {
                    bool active = gm.currentTurnColor == GameManager.PlayerColor.RED || gm.currentTurnColor == GameManager.PlayerColor.YELLOW;
                    cards[0].SetActive(active, pulseSpeed);
                    cards[0].SetTokensHome(TokensHome(GameManager.PlayerColor.RED) + TokensHome(GameManager.PlayerColor.YELLOW));
                }
                if (cards.Length > 1 && cards[1] != null && cards[1].gameObject.activeSelf)
                {
                    bool active = gm.currentTurnColor == GameManager.PlayerColor.GREEN || gm.currentTurnColor == GameManager.PlayerColor.BLUE;
                    cards[1].SetActive(active, pulseSpeed);
                    cards[1].SetTokensHome(TokensHome(GameManager.PlayerColor.GREEN) + TokensHome(GameManager.PlayerColor.BLUE));
                }
                return;
            }

            for (int i = 0; i < cards.Length && i < Order.Length; i++)
            {
                var card = cards[i];
                if (card == null || !card.gameObject.activeSelf) continue;

                bool active = gm.currentTurnColor == Order[i];
                card.SetActive(active, pulseSpeed);
                card.SetTokensHome(TokensHome(Order[i]));
            }
        }

        private int TokensHome(GameManager.PlayerColor colour)
        {
            if (_tokens == null) return 0;
            int n = 0;
            foreach (var t in _tokens)
            {
                if (t != null && t.tokenColor == colour && t.isFinished) n++;
            }
            return n;
        }

        private void PaintTurn(GameManager gm)
        {
            if (turnLabel == null) return;

            bool mine = gm.IsHumanControlled(gm.currentTurnColor);
            string text;

            if (gm.isRolling || gm.currentPhase == GameManager.TurnPhase.Rolling) text = "ROLLING…";
            else if (!mine) text = $"{gm.currentTurnColor}'S TURN";
            else if (gm.currentPhase == GameManager.TurnPhase.AwaitingSelection) text = "SELECT A TOKEN";
            else if (gm.currentPhase == GameManager.TurnPhase.Moving) text = "MOVING…";
            else text = "YOUR TURN";

            if (text != _lastTurnLabel)
            {
                _lastTurnLabel = text;
                turnLabel.text = text;
                turnLabel.gameObject.SetActive(!string.IsNullOrEmpty(text));
            }
        }

        private void PaintRoll(GameManager gm)
        {
            if (rollButton == null) return;

            bool mine = gm.IsHumanControlled(gm.currentTurnColor);
            bool canRoll = mine
                           && !gm.isRolling
                           && !gm.hasRolled
                           && gm.currentPhase == GameManager.TurnPhase.AwaitingRoll;

            rollButton.interactable = canRoll;
            rollButton.transition = Selectable.Transition.None;
            if (rollBackground != null && rollEnabled != null)
                rollBackground.sprite = rollEnabled; // Always keep the solid 3D play button

            // Turn ash/faded when inactive/waiting/not player's turn to roll;
            // restore vibrant crisp crimson (Color.white on crimson sprite) when it's the player's turn to roll:
            Color targetColor = canRoll ? Color.white : new Color(0.55f, 0.55f, 0.55f, 0.70f);
            if (rollBackground != null)
                rollBackground.color = targetColor;

            float targetAlpha = canRoll ? 1f : 0.70f;
            if (rollGroup != null)
                rollGroup.alpha = Mathf.MoveTowards(rollGroup.alpha, targetAlpha, Time.unscaledDeltaTime * 6f);

            if (rollLabel != null)
            {
                rollLabel.text = "ROLL";
                rollLabel.color = canRoll ? Color.white : new Color(0.85f, 0.85f, 0.85f, 0.75f);
            }
            if (rollSubLabel != null)
            {
                rollSubLabel.text = "";
                rollSubLabel.gameObject.SetActive(false);
            }
        }

        private void PaintTimer(GameManager gm)
        {
            // Non-online game modes (VS Computer and Pass & Play) do NOT need a countdown timer.
            // Only online mode (QuickMatch) gets a turn timer countdown per design rules.
            var session = LudoNXSession.Instance;
            bool isOnline = session != null && session.pendingMode == LudoNXSession.Mode.QuickMatch;
            if (!isOnline)
            {
                if (turnTimerRoot != null) turnTimerRoot.gameObject.SetActive(false);
                return;
            }

            if (turnTimerRoot != null) turnTimerRoot.gameObject.SetActive(turnSeconds > 0f);
            if (turnSeconds <= 0f || turnTimerLabel == null) return;

            bool mine = gm.IsHumanControlled(gm.currentTurnColor);
            if (turnTimerGroup != null) turnTimerGroup.alpha = mine ? 1f : 0.85f;

            float left = Mathf.Max(0f, turnSeconds - (Time.unscaledTime - _turnStarted));
            int seconds = Mathf.CeilToInt(left);
            turnTimerLabel.text = $"0:{seconds:00}";

            // Under 10s: digits go bright red #FF5046 and pill pulses 1.00 to 1.04 once per second
            if (left <= 10f)
            {
                turnTimerLabel.color = new Color32(255, 80, 70, 255); // #FF5046
                if (turnTimerRoot != null)
                {
                    float pulse = 1.00f + 0.04f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * Mathf.PI));
                    turnTimerRoot.localScale = new Vector3(pulse, pulse, 1f);
                }
            }
            else
            {
                turnTimerLabel.color = new Color32(255, 242, 220, 255); // cream #FFF2DC
                if (turnTimerRoot != null) turnTimerRoot.localScale = Vector3.one;
            }

            if (left > 0f || _autoRolled) return;

            if (mine && !gm.hasRolled && !gm.isRolling && gm.currentPhase == GameManager.TurnPhase.AwaitingRoll)
            {
                _autoRolled = true;
                gm.OnRollButtonClicked();
            }
        }

        /// <summary>The stake this match is worth, formatted per bible 5.13 and MATCH_HUD_V2.md.</summary>
        public void SetStake(int multiplier, int baseNx)
        {
            if (stakePlaque != null)
            {
                // Hide stake plaque entirely when there is no stake (Pass & Play, or multiplier <= 1)
                bool hasStake = multiplier > 1;
                stakePlaque.SetActive(hasStake);
            }

            if (stakeLabel == null) return;
            if (multiplier <= 1)
            {
                stakeLabel.text = $"+{baseNx} NX";
                return;
            }
            stakeLabel.text = $"+{baseNx * multiplier} NX x X{multiplier}";
        }

        // ==================================================================================
        // input
        // ==================================================================================

        private void OnRoll()
        {
            if (rollButton != null) rollButton.interactable = false;
            if (rollBackground != null) rollBackground.color = new Color(0.55f, 0.55f, 0.55f, 0.70f);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayDiceRoll();
            if (GameManager.Instance != null) GameManager.Instance.OnRollButtonClicked();
        }

        private void OnPause()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButton();
            if (MatchBridge.Instance != null) MatchBridge.Instance.OpenPause();
            else UIModal.Open("Pause");
        }

        private void OnRules()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButton();
            RulesModal.Open();
        }

        private void OnToggleMute()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButton();
            bool currentlyMuted = !LudoNX.UI.SettingsService.Sound && !LudoNX.UI.SettingsService.Music;
            bool nextState = currentlyMuted; // if muted, unmute (true); if unmuted, mute (false)
            LudoNX.UI.SettingsService.Sound = nextState;
            LudoNX.UI.SettingsService.Music = nextState;
            UpdateMuteVisuals();
        }

        private void UpdateMuteVisuals()
        {
            if (muteIcon == null) return;
            bool isMuted = !LudoNX.UI.SettingsService.Sound && !LudoNX.UI.SettingsService.Music;
            if (soundOnIcon != null && soundOffIcon != null)
                muteIcon.sprite = isMuted ? soundOffIcon : soundOnIcon;
        }

        private UIModal _openTray;
        private System.Type _openTrayType;

        private void OnEmoji()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButton();
            ToggleTray<MatchEmoteTray>("MatchEmoteTray", t => t.Configure(this));
        }

        private void OnMessage()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButton();
            ToggleTray<MatchMessageTray>("MatchMessageTray", t => t.Configure(this));
        }

        /// <summary>Opens the requested tray, closing whichever tray (if any) is already open —
        /// tapping the same action-bar button again closes its own tray instead of stacking a
        /// second copy. Clears _openTray from UIModal's own dismiss callback, so it stays correct
        /// whether the tray closed itself (fired, re-tapped) or something else dismissed it.</summary>
        private void ToggleTray<T>(string prefabName, System.Action<T> configure) where T : UIModal
        {
            if (_openTrayType == typeof(T))
            {
                _openTray?.Dismiss();
                _openTray = null;
                _openTrayType = null;
                return;
            }

            _openTray?.Dismiss();
            var modal = UIModal.Open(prefabName, () => { _openTray = null; _openTrayType = null; });
            if (modal is T typed) configure(typed);
            _openTray = modal;
            _openTrayType = typeof(T);
        }

        /// <summary>Which house is "you" for firing an emote or message. Against AI it is always
        /// the fixed human seat, regardless of whose turn it is — reacting is social, not a game
        /// action. In Pass and Play every seat is a person on this device, so it is whoever
        /// currently holds the phone: the seat on turn.</summary>
        public GameManager.PlayerColor LocalSender()
        {
            var gm = GameManager.Instance;
            return gm.aiPlaysOtherColors ? gm.humanColor : gm.currentTurnColor;
        }

        // ==================================================================================
        // shared mute list — one mute covers both trays (match-emoji-tray.json, match-message-tray.json)
        // ==================================================================================

        private readonly HashSet<GameManager.PlayerColor> _mutedHouses = new HashSet<GameManager.PlayerColor>();

        public bool IsMuted(GameManager.PlayerColor house) => _mutedHouses.Contains(house);

        /// <summary>Wired to every card's long-press. Never mutes the local human.</summary>
        public void ToggleMute(GameManager.PlayerColor house)
        {
            var gm = GameManager.Instance;
            if (gm != null && house == gm.humanColor) return;

            bool nowMuted = !_mutedHouses.Contains(house);
            if (nowMuted) _mutedHouses.Add(house); else _mutedHouses.Remove(house);

            var card = CardFor(house);
            card?.SetMuted(nowMuted);
        }

        public MatchPlayerCard CardFor(GameManager.PlayerColor house)
        {
            if (cards == null) return null;
            foreach (var c in cards)
                if (c != null && c.house == house) return c;
            return null;
        }
    }
}
