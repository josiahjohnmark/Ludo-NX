using System.Collections.Generic;
using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// The seam between the new interface and the existing match code.
    ///
    /// The old <c>LudoMenuFlow</c> did navigation and match lifecycle in one class, which is why
    /// it had to own every screen reference. This does only the lifecycle half: hand it the
    /// pending config and it puts the board on screen and starts a game. Navigation is the
    /// ScreenManager's job, and neither knows about the other's internals.
    /// </summary>
    [DisallowMultipleComponent]
    public class MatchBridge : MonoBehaviour
    {
        public static MatchBridge Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Object.FindAnyObjectByType<MatchBridge>();
                    if (_instance == null)
                    {
                        var go = new GameObject("MatchBridge");
                        _instance = go.AddComponent<MatchBridge>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
            private set { _instance = value; }
        }
        private static MatchBridge _instance;

        internal static void ResetStatics() { _instance = null; }

        [Tooltip("The existing in-match HUD canvas (player cards, roll button, timer).")]
        public GameObject hudCanvas;

        [Tooltip("Board, tokens and dice. Leave empty if the board is always visible.")]
        public GameObject boardRoot;

        public bool MatchInProgress { get; private set; }

        private float _startedAt;

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(this); return; }
            _instance = this;
            if (hudCanvas == null) hudCanvas = FindHudCanvas();
        }

        private void OnEnable() { LudoMenuFlow.MatchEnded += HandleMatchEnded; }
        private void OnDisable() { LudoMenuFlow.MatchEnded -= HandleMatchEnded; }

        /// <summary>
        /// The match is over. Bank the statistics that <c>PointsManager</c> is about to discard,
        /// hand the screen back to the menu stack, and show the result.
        /// </summary>
        private void HandleMatchEnded(List<GameManager.PlayerColor> leaderBoard)
        {
            MatchInProgress = false;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayMenuMusic();
            RecordStats(leaderBoard);

            HideHud();
            if (hudCanvas != null) hudCanvas.SetActive(false);
            if (ScreenManager.Instance == null) return;
            ScreenManager.Instance.SetScreensVisible(true);
            ScreenManager.Instance.Replace(ScreenId.Result, leaderBoard);
        }

        private void RecordStats(List<GameManager.PlayerColor> leaderBoard)
        {
            var gm = GameManager.Instance;
            var points = PointsManager.Instance;
            var session = LudoNXSession.Instance;
            if (gm == null || leaderBoard == null) return;

            GameManager.PlayerColor me = gm.humanColor;
            int placement = leaderBoard.IndexOf(me) + 1;
            if (placement <= 0) placement = leaderBoard.Count;

            int score = 0, captures = 0, taken = 0, home = 0;
            bool mvp = false;
            if (points != null)
            {
                var s = points.GetStats(me);
                if (s != null)
                {
                    score = s.points;
                    captures = s.captures;
                    taken = s.timesCaptured;
                    home = s.tokensHome;
                }
                mvp = points.DetermineMVP() == me;
            }

            int playerCount = session != null ? session.pendingPlayerCount : leaderBoard.Count;
            int duration = _startedAt > 0f ? Mathf.RoundToInt(Time.unscaledTime - _startedAt) : 0;

            StatsService.RecordMatch(placement, playerCount, score, captures, taken, home, mvp, duration);
            MissionService.EndMatch(placement == 1);

            // The existing session still owns coins and NX; keep it in step. Pass-and-play is
            // never staked, so it always settles at x1 however the setup screen was left.
            if (session != null)
            {
                int stake = session.pendingMode == LudoNXSession.Mode.PassAndPlay
                    ? 1
                    : session.pendingMultiplier;
                session.RecordMatchResult(placement == 1, score, true, stake);
                StatsService.RecordNx(session.LastNXEarned);
            }
        }

        /// <summary>
        /// Finds the in-match HUD canvas.
        ///
        /// Deliberately NOT "the object with UIManager on it" — in this project UIManager is a
        /// component of the GameManager object, not of the canvas, so that search returns the
        /// game's brain and hiding it kills the match. Look for an actual Canvas instead.
        /// </summary>
        private static GameObject FindHudCanvas()
        {
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            // Preference 1: the canvas carrying the HUD layout driver.
            foreach (var c in canvases)
                if (c.GetComponent<MatchHudLayout>() != null) return c.gameObject;

            // Preference 2: the canvas that owns the match-only visual helpers.
            foreach (var c in canvases)
                if (c.GetComponent<BackgroundAmbience>() != null || c.GetComponent<FloatingTextFX>() != null)
                    return c.gameObject;

            // Preference 3: by name, excluding our own root and the legacy menu.
            foreach (var c in canvases)
                if (c.name == "Canvas") return c.gameObject;

            return null;
        }

        /// <summary>
        /// Starts a match from whatever <see cref="LudoNXSession"/> currently has pending.
        /// Mirrors what LudoMenuFlow.StartMatch did, minus the navigation.
        /// </summary>
        /// <param name="hideMenus">
        /// False leaves the menu on screen so the caller can fade it out itself and reveal the
        /// board underneath, instead of cutting to it. The pre-match countdown uses that;
        /// everything else wants the default.
        /// </param>
        public void StartPendingMatch(bool hideMenus = true)
        {
            var gm = GameManager.Instance;
            if (gm == null)
            {
                Debug.LogError("[MatchBridge] No GameManager in the scene — cannot start a match.");
                return;
            }

            var session = LudoNXSession.Instance;
            LudoAI.Difficulty difficulty = session != null ? session.pendingDifficulty : LudoAI.Difficulty.Normal;

            // Set before initialising so the very first AI turn already uses it.
            LudoAI.CurrentDifficulty = difficulty;

            MatchInProgress = true;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayMatchMusic();
            _startedAt = Time.unscaledTime;
            MissionService.BeginMatch();
            if (boardRoot != null) boardRoot.SetActive(true);

            // The old HUD stays off: it is the previous art style and still points at images that
            // were deleted from the project, which is why the board looked bare.
            if (hudCanvas != null) hudCanvas.SetActive(false);
            if (hideMenus && ScreenManager.Instance != null) ScreenManager.Instance.SetScreensVisible(false);

            if (session != null)
            {
                List<GameManager.PlayerColor> players = session.BuildPendingPlayers();

                // Entry zero is the human, and BuildPendingPlayers has already rotated the chosen
                // colour to the front — so picking green really does hand the player green.
                gm.humanColor = players[0];

                // Pass-and-play has no AI at all: every house is a person on this device.
                gm.aiPlaysOtherColors = !session.AllHousesHuman;

                gm.InitializeMatch(players, players[0]);
                ShowHud();
                Debug.Log("[MatchBridge] Match started — " + string.Join(", ", players) +
                          ", human is " + players[0] +
                          (session.AllHousesHuman ? ", pass-and-play (no AI)." : ", " + difficulty + " AI."));
            }
            else
            {
                gm.InitializeMatchWithPlayerCount(4);
                ShowHud();
                Debug.Log("[MatchBridge] Match started — 4 players, " + difficulty + " AI (no session).");
            }
        }

        private MatchHudView _hud;

        /// <summary>
        /// Spawns the current HUD and binds it to the match that has just been initialised.
        /// Created per match rather than kept in the scene so it always reflects the houses that
        /// are actually playing, and so nothing of it survives into the menu.
        /// </summary>
        private void ShowHud()
        {
            if (_hud == null)
            {
                var prefab = Resources.Load<GameObject>("UI/Components/MatchHud");
                if (prefab == null)
                {
                    Debug.LogError("[MatchBridge] Resources/UI/Components/MatchHud is missing — no HUD this match.");
                    return;
                }
                _hud = Instantiate(prefab).GetComponent<MatchHudView>();
                _hud.gameObject.name = "MatchHud";
            }

            _hud.gameObject.SetActive(true);
            _hud.Bind();

            var session = LudoNXSession.Instance;
            _hud.SetStake(session != null ? session.pendingMultiplier : 1, 20);
        }

        private void HideHud()
        {
            if (_hud != null) _hud.gameObject.SetActive(false);
        }

        private UIModal _pause;

        private void Update()
        {
            // During a match the back gesture belongs to the pause dialog, not the menu stack.
            if (!MatchInProgress || !Input.GetKeyDown(KeyCode.Escape)) return;
            if (_pause != null) return;
            OpenPause();
        }

        /// <summary>Resume / Quit Match. Also reachable from the HUD's pause button.</summary>
        public void OpenPause()
        {
            if (_pause != null) return;
            _pause = BuildRuntimePauseModal();
        }

        private UIModal BuildRuntimePauseModal()
        {
            Transform parent = null;
            if (_hud != null)
                parent = _hud.transform;
            else if (ScreenManager.Instance != null && ScreenManager.Instance.ModalRoot != null)
                parent = ScreenManager.Instance.ModalRoot;
            else
            {
                var canvas = Object.FindAnyObjectByType<Canvas>();
                if (canvas != null) parent = canvas.transform;
            }

            var rootObj = new GameObject("PauseModal", typeof(RectTransform), typeof(CanvasGroup), typeof(ConfirmModal));
            if (parent != null) rootObj.transform.SetParent(parent, false);
            var rootRt = (RectTransform)rootObj.transform;
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            var modal = rootObj.GetComponent<ConfirmModal>();
            modal.dismissOnScrimTap = true;
            modal.SetDismissCallback(() => _pause = null);

            var fontSet = UIFontSet.Current;
            TMPro.TMP_FontAsset displayFont = fontSet != null && fontSet.display != null ? fontSet.display : null;
            TMPro.TMP_FontAsset bodyFont = fontSet != null && fontSet.body != null ? fontSet.body : null;

            // Scrim (dark semi-transparent backdrop)
            var scrimObj = new GameObject("Scrim", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            scrimObj.transform.SetParent(rootObj.transform, false);
            var scrimRt = (RectTransform)scrimObj.transform;
            scrimRt.anchorMin = Vector2.zero;
            scrimRt.anchorMax = Vector2.one;
            scrimRt.offsetMin = new Vector2(-2500, -2500);
            scrimRt.offsetMax = new Vector2(2500, 2500);
            var scrimImg = scrimObj.GetComponent<UnityEngine.UI.Image>();
            scrimImg.color = new Color(0.043f, 0.051f, 0.071f, 0.72f);
            var scrimBtn = scrimObj.GetComponent<UnityEngine.UI.Button>();
            scrimBtn.transition = UnityEngine.UI.Selectable.Transition.None;
            scrimBtn.onClick.AddListener(() =>
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayButton();
                modal.Dismiss();
                _pause = null;
            });

            // Card (chassis card_shell with Lucky Six corner dice)
            var cardObj = new GameObject("Card", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            cardObj.transform.SetParent(rootObj.transform, false);
            var cardRt = (RectTransform)cardObj.transform;
            cardRt.anchorMin = cardRt.anchorMax = cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(340f, 275f);
            cardRt.anchoredPosition = new Vector2(0f, 10f);
            var cardImg = cardObj.GetComponent<UnityEngine.UI.Image>();
            var cardSprite = Resources.Load<Sprite>("UI/Cards/card_shell");
#if UNITY_EDITOR
            if (cardSprite == null) cardSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/MatchSetup/Cards/card_shell.png");
#endif
            if (cardSprite != null)
            {
                cardImg.sprite = cardSprite;
                cardImg.type = UnityEngine.UI.Image.Type.Sliced;
                cardImg.pixelsPerUnitMultiplier = 3.5f;
                cardImg.color = Color.white; // Pure white so cream card art displays natively
            }
            else
            {
                cardImg.color = new Color32(248, 244, 236, 255);
            }
            cardImg.raycastTarget = true;
            modal.card = cardRt;

            // Nameplate straddling top
            var plateObj = new GameObject("Nameplate", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            plateObj.transform.SetParent(cardObj.transform, false);
            var plateRt = (RectTransform)plateObj.transform;
            plateRt.anchorMin = plateRt.anchorMax = new Vector2(0.5f, 1f);
            plateRt.pivot = new Vector2(0.5f, 0.5f);
            plateRt.sizeDelta = new Vector2(170f, 38f);
            plateRt.anchoredPosition = Vector2.zero;
            var plateImg = plateObj.GetComponent<UnityEngine.UI.Image>();
            var pillSprite = Resources.Load<Sprite>("UI/Cards/pill_cream");
#if UNITY_EDITOR
            if (pillSprite == null) pillSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/MatchSetup/Cards/pill_cream.png");
#endif
            if (pillSprite != null)
            {
                plateImg.sprite = pillSprite;
                plateImg.type = UnityEngine.UI.Image.Type.Sliced;
                plateImg.pixelsPerUnitMultiplier = 3.5f;
                plateImg.color = Color.white;
            }
            else
            {
                plateImg.color = new Color32(245, 235, 218, 255);
            }

            // Title "PAUSED"
            var titleObj = new GameObject("Title", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            titleObj.transform.SetParent(plateObj.transform, false);
            var titleRt = (RectTransform)titleObj.transform;
            titleRt.anchorMin = Vector2.zero;
            titleRt.anchorMax = Vector2.one;
            titleRt.offsetMin = Vector2.zero;
            titleRt.offsetMax = Vector2.zero;
            var titleLbl = titleObj.GetComponent<TMPro.TextMeshProUGUI>();
            if (displayFont != null) titleLbl.font = displayFont;
            titleLbl.text = "PAUSED";
            titleLbl.fontSize = 19f;
            titleLbl.fontStyle = TMPro.FontStyles.Bold;
            titleLbl.alignment = TMPro.TextAlignmentOptions.Center;
            titleLbl.color = new Color32(30, 22, 12, 255); // #1E160C
            titleLbl.characterSpacing = 2f;
            modal.titleLabel = titleLbl;

            // Body "The match is waiting for you."
            var bodyObj = new GameObject("Body", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            bodyObj.transform.SetParent(cardObj.transform, false);
            var bodyRt = (RectTransform)bodyObj.transform;
            bodyRt.anchorMin = new Vector2(0.5f, 1f);
            bodyRt.anchorMax = new Vector2(0.5f, 1f);
            bodyRt.pivot = new Vector2(0.5f, 0.5f);
            bodyRt.sizeDelta = new Vector2(280f, 40f);
            bodyRt.anchoredPosition = new Vector2(0f, -56f);
            var bodyLbl = bodyObj.GetComponent<TMPro.TextMeshProUGUI>();
            if (bodyFont != null) bodyLbl.font = bodyFont;
            else if (displayFont != null) bodyLbl.font = displayFont;
            bodyLbl.text = "The match is waiting for you.";
            bodyLbl.fontSize = 14.5f;
            bodyLbl.alignment = TMPro.TextAlignmentOptions.Center;
            bodyLbl.color = new Color(0.28f, 0.22f, 0.16f, 0.95f);
            modal.bodyLabel = bodyLbl;

            // RESUME Button (emerald green hero button)
            var resumeObj = new GameObject("ResumeButton", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            resumeObj.transform.SetParent(cardObj.transform, false);
            var resumeRt = (RectTransform)resumeObj.transform;
            resumeRt.anchorMin = resumeRt.anchorMax = resumeRt.pivot = new Vector2(0.5f, 0.5f);
            resumeRt.sizeDelta = new Vector2(240f, 54f);
            resumeRt.anchoredPosition = new Vector2(0f, -12f);
            var resumeImg = resumeObj.GetComponent<UnityEngine.UI.Image>();
            var btnSprite = Resources.Load<Sprite>("UI/Cards/play_btn");
#if UNITY_EDITOR
            if (btnSprite == null) btnSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/MatchSetup/Cards/play_btn.png");
#endif
            if (btnSprite != null)
            {
                resumeImg.sprite = btnSprite;
                resumeImg.type = UnityEngine.UI.Image.Type.Sliced;
                resumeImg.pixelsPerUnitMultiplier = 2f;
                resumeImg.color = new Color32(40, 160, 50, 255); // emerald green #28A032
            }
            else
            {
                resumeImg.color = new Color32(40, 160, 50, 255);
            }
            var resumeBtn = resumeObj.GetComponent<UnityEngine.UI.Button>();
            modal.cancelButton = resumeBtn;

            var resumeLblObj = new GameObject("Label", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            resumeLblObj.transform.SetParent(resumeObj.transform, false);
            var resumeLblRt = (RectTransform)resumeLblObj.transform;
            resumeLblRt.anchorMin = Vector2.zero;
            resumeLblRt.anchorMax = Vector2.one;
            resumeLblRt.offsetMin = Vector2.zero;
            resumeLblRt.offsetMax = Vector2.zero;
            var resumeLbl = resumeLblObj.GetComponent<TMPro.TextMeshProUGUI>();
            if (displayFont != null) resumeLbl.font = displayFont;
            resumeLbl.text = "RESUME";
            resumeLbl.fontSize = 20f;
            resumeLbl.fontStyle = TMPro.FontStyles.Bold;
            resumeLbl.alignment = TMPro.TextAlignmentOptions.Center;
            resumeLbl.color = Color.white;
            resumeLbl.characterSpacing = 1.5f;
            modal.cancelLabel = resumeLbl;

            // QUIT MATCH Button (quiet secondary action)
            var quitObj = new GameObject("QuitButton", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            quitObj.transform.SetParent(cardObj.transform, false);
            var quitRt = (RectTransform)quitObj.transform;
            quitRt.anchorMin = new Vector2(0.5f, 0f);
            quitRt.anchorMax = new Vector2(0.5f, 0f);
            quitRt.pivot = new Vector2(0.5f, 0.5f);
            quitRt.sizeDelta = new Vector2(210f, 40f);
            quitRt.anchoredPosition = new Vector2(0f, 42f);
            var quitImg = quitObj.GetComponent<UnityEngine.UI.Image>();
            if (pillSprite != null)
            {
                quitImg.sprite = pillSprite;
                quitImg.type = UnityEngine.UI.Image.Type.Sliced;
                quitImg.pixelsPerUnitMultiplier = 3.5f;
                quitImg.color = new Color32(242, 232, 218, 255);
            }
            else
            {
                quitImg.color = new Color32(230, 220, 205, 200);
            }
            var quitBtn = quitObj.GetComponent<UnityEngine.UI.Button>();
            modal.confirmButton = quitBtn;

            var quitLblObj = new GameObject("Label", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            quitLblObj.transform.SetParent(quitObj.transform, false);
            var quitLblRt = (RectTransform)quitLblObj.transform;
            quitLblRt.anchorMin = Vector2.zero;
            quitLblRt.anchorMax = Vector2.one;
            quitLblRt.offsetMin = Vector2.zero;
            quitLblRt.offsetMax = Vector2.zero;
            var quitLbl = quitLblObj.GetComponent<TMPro.TextMeshProUGUI>();
            if (displayFont != null) quitLbl.font = displayFont;
            quitLbl.text = "QUIT MATCH";
            quitLbl.fontSize = 14f;
            quitLbl.fontStyle = TMPro.FontStyles.Bold;
            quitLbl.alignment = TMPro.TextAlignmentOptions.Center;
            quitLbl.color = new Color32(170, 35, 35, 255);
            quitLbl.characterSpacing = 1.2f;
            modal.confirmLabel = quitLbl;

            // Close button below card
            var closeObj = new GameObject("CloseButton", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            closeObj.transform.SetParent(cardObj.transform, false);
            var closeRt = (RectTransform)closeObj.transform;
            closeRt.anchorMin = new Vector2(0.5f, 0f);
            closeRt.anchorMax = new Vector2(0.5f, 0f);
            closeRt.pivot = new Vector2(0.5f, 0.5f);
            closeRt.sizeDelta = new Vector2(36f, 36f);
            closeRt.anchoredPosition = new Vector2(0f, -32f);
            var closeImg = closeObj.GetComponent<UnityEngine.UI.Image>();
            var closeSprite = Resources.Load<Sprite>("UI/Cards/close_btn");
#if UNITY_EDITOR
            if (closeSprite == null) closeSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/MatchSetup/Cards/close_btn.png");
#endif
            if (closeSprite != null)
            {
                closeImg.sprite = closeSprite;
                closeImg.preserveAspect = true;
            }
            var closeBtn = closeObj.GetComponent<UnityEngine.UI.Button>();
            closeBtn.onClick.AddListener(() =>
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayButton();
                modal.Dismiss();
                _pause = null;
            });

            modal.Configure("PAUSED", "The match is waiting for you.",
                            "QUIT MATCH", "RESUME",
                            () =>
                            {
                                _pause = null;
                                LeaveMatch();
                                if (ScreenManager.Instance != null)
                                    ScreenManager.Instance.ResetTo(ScreenId.Home);
                            });

            modal.PlayOpen();
            return modal;
        }

        /// <summary>Tears the match down and hands the screen back to the menu stack.</summary>
        public void LeaveMatch()
        {
            MatchInProgress = false;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayMenuMusic();
            HideHud();
            if (hudCanvas != null) hudCanvas.SetActive(false);
            if (boardRoot != null) boardRoot.SetActive(false);
            if (ScreenManager.Instance != null) ScreenManager.Instance.SetScreensVisible(true);
        }
    }
}
