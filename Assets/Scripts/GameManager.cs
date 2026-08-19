using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum PlayerColor
    {
        RED,
        GREEN,
        YELLOW,
        BLUE
    }

    [Header("Script References")]
    public Dice3DRoller physicsRoller;

    [Header("Turn & Player State")]
    public PlayerColor currentTurnColor = PlayerColor.RED;
    public List<PlayerColor> activePlayers = new List<PlayerColor>();

    [Header("Control")]
    [Tooltip("The colour the player controls. Every other active colour is played by AIController.")]
    public PlayerColor humanColor = PlayerColor.RED;
    [Tooltip("Off = all four colours are hot-seat human, for testing.")]
    public bool aiPlaysOtherColors = true;

    public bool IsHumanControlled(PlayerColor color)
    {
        return !aiPlaysOtherColors || color == humanColor;
    }

    [Header("Leaderboard & Match State")]
    public List<PlayerColor> leaderBoard = new List<PlayerColor>();

    [Header("Dice GameObjects (assign the 3D dice meshes here)")]
    public GameObject dice1Object;
    public GameObject dice2Object;

    [Header("Roll Values & Selection States")]
    public int dice1Value = 1;
    public int dice2Value = 1;

    public int dice1 { get => dice1Value; set => dice1Value = value; }
    public int dice2 { get => dice2Value; set => dice2Value = value; }

    public bool isDice1Selected = false;
    public bool isDice2Selected = false;
    public bool isDiceCombined = false;
    public int selectedDiceValue = 0;

    [Header("Roll States & Doubles Penalty")]
    public bool isRolling = false;
    public bool hasRolled = false;
    public int consecutiveDoublesCount = 0;

    /// <summary>
    /// Explicit turn phases. Every player input is gated on one of these, which is what stops a
    /// stale coroutine or a timeout from acting on a turn that has already moved on.
    /// </summary>
    public enum TurnPhase
    {
        AwaitingRoll,       // Roll button live, tokens dead
        Rolling,            // Dice animating, nothing accepted
        AwaitingSelection,  // Player picks a die, then a token
        Moving,             // A token is animating, nothing accepted
        Resolving           // Post-move bookkeeping, nothing accepted
    }

    [Header("Turn Phase")]
    public TurnPhase currentPhase = TurnPhase.AwaitingRoll;

    /// <summary>
    /// Bumped every time the turn changes hands or restarts. Any coroutine that was in flight
    /// across that boundary compares its captured value and aborts instead of writing state that
    /// belongs to the previous turn — the specific fault that left the roll button dead.
    /// </summary>
    private int turnSequence = 0;
    public int TurnSequence => turnSequence;

    /// <summary>
    /// Exactly one token may animate at a time, match-wide. TokenMovement's own isMoving flag is
    /// per-token, so two tokens could previously animate together and both consume a die.
    /// </summary>
    public bool IsAnyTokenMoving { get; private set; }

    private bool grantExtraTurn = false;
    private string extraTurnReason = null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Application.runInBackground = true;
    }

    private void Start()
    {
        // The menu owns match setup when it is present: it decides player count and AI difficulty
        // and calls InitializeMatchWithPlayerCount itself. Auto-starting here as well would deal a
        // 4-player match underneath the home screen and then deal a second one on top of it.
        if (LudoMenuFlow.Instance != null) return;

        // Same contract for the new interface. Without this line, retiring LudoMenuFlow makes the
        // guard above fall through and a match is dealt under the home screen on the first frame.
        if (LudoNX.UI.ScreenManager.Instance != null) return;

        // No menu in the scene — keep the old behaviour so the board scene stays playable alone.
        if (activePlayers == null || activePlayers.Count == 0)
        {
            InitializeMatchWithPlayerCount(4);
            Debug.Log("Auto-initialized 4-player match (no menu flow in scene).");
        }
    }

    private void Update()
    {
        WatchMoveLock();

#if UNITY_EDITOR
        // Editor-only convenience. Goes through the same gate as the button so it can't desync
        // the turn machine, and never ships to a device.
        if (Input.GetKeyDown(KeyCode.Space))
        {
            OnRollButtonClicked();
        }
#endif
    }

    #region Move-lock watchdog

    [Header("Safety")]
    [Tooltip("Seconds the move lock may stay held with no token actually moving before it is force-released.")]
    public float stuckMoveLockTimeout = 3f;

    private float moveLockHeldFor;
    private float nextLockCheck;

    /// <summary>
    /// Releases the match-wide move lock if it is held while nothing is actually moving.
    ///
    /// TryBeginTokenMove and EndTokenMove bracket a coroutine. If anything between them throws, the
    /// coroutine dies mid-flight and the lock is never released — and because AIController refuses
    /// to act while the lock is held, the whole match freezes with no way back. A single unlucky
    /// exception should not be able to end a game permanently.
    ///
    /// This is a net, not a fix: it logs loudly so the underlying fault is still visible rather
    /// than being quietly papered over. The timeout is comfortably longer than the longest hop
    /// (maxTotalMoveDuration is 1.25s), so a slow move can never trip it.
    /// </summary>
    private void WatchMoveLock()
    {
        if (!IsAnyTokenMoving)
        {
            moveLockHeldFor = 0f;
            return;
        }

        // Scanning tokens is only worth doing a couple of times a second.
        if (Time.time < nextLockCheck) return;
        nextLockCheck = Time.time + 0.25f;

        bool somethingIsMoving = false;
        foreach (TokenMovement token in FindObjectsByType<TokenMovement>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (token.IsMoving) { somethingIsMoving = true; break; }
        }

        if (somethingIsMoving)
        {
            moveLockHeldFor = 0f;
            return;
        }

        moveLockHeldFor += 0.25f;
        if (moveLockHeldFor < stuckMoveLockTimeout) return;

        Debug.LogWarning($"[GameManager] Move lock was held for {moveLockHeldFor:0.0}s with no token moving — " +
                         $"a move coroutine died before releasing it. Force-releasing and resuming " +
                         $"{currentTurnColor}'s turn (phase was {currentPhase}).");

        moveLockHeldFor = 0f;
        EndTokenMove();

        // Finish the step the dead coroutine never completed: spend the die and either hand the
        // player their second die or pass the turn on.
        if (currentPhase == TurnPhase.Moving)
        {
            ConsumeSelectedDice();
        }
    }

    #endregion

    #region Match Initialization

    public void InitializeMatchWithPlayerCount(int playerCount)
    {
        List<PlayerColor> players = new List<PlayerColor>();

        if (playerCount >= 4)
        {
            players.Add(PlayerColor.RED);
            players.Add(PlayerColor.GREEN);
            players.Add(PlayerColor.YELLOW);
            players.Add(PlayerColor.BLUE);
        }
        else
        {
            players.Add(PlayerColor.RED);
            players.Add(PlayerColor.YELLOW);
        }

        InitializeMatch(players, PlayerColor.RED);
    }

    public void InitializeMatch(List<PlayerColor> players, PlayerColor startingColor)
    {
        activePlayers = new List<PlayerColor>(players);
        currentTurnColor = activePlayers.Contains(startingColor) ? startingColor : activePlayers[0];
        leaderBoard.Clear();
        ResetDiceState();

        // A human colour that is not in the match means every house is AI-driven and the player
        // watches their own game. Guarding here rather than at the call site means no future caller
        // can reintroduce it.
        if (!activePlayers.Contains(humanColor))
        {
            humanColor = activePlayers[0];
            Debug.Log($"[GameManager] Human colour was not in this match — moved to {humanColor}.");
        }

        turnSequence++;
        currentPhase = TurnPhase.AwaitingRoll;
        IsAnyTokenMoving = false;
        ClearExtraTurn();
        ResetAllTokensToYard();
        ApplyActivePlayerVisibility();

        // Every token is back in its own yard slot, so clear any stack scaling left over from the
        // previous match. Instant, not animated — nothing should be seen sliding at match start.
        TokenStackLayout.Refresh(false);

        if (PointsManager.Instance != null)
        {
            PointsManager.Instance.InitializeForMatch(activePlayers);
        }

        if (BackgroundAmbience.Instance != null)
        {
            BackgroundAmbience.Instance.TransitionToPlayerColor(currentTurnColor);
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ApplyActivePlayerCards();
            UIManager.Instance.UpdateTurnUI(currentTurnColor, "ROLL DICE");
            UIManager.Instance.StartTurnTimer();
        }

        Debug.Log($"Match initialized. Players: {string.Join(", ", activePlayers)}. " +
                  $"Starting: {currentTurnColor}. Human: {humanColor}.");
    }

    /// <summary>
    /// Hides every house that is not in this match, tokens and all.
    ///
    /// In a two-player game the two unused yards are then visibly empty, which states who is
    /// playing more clearly than any label could. Leaving four sets of tokens on the board and
    /// simply refusing to move two of them reads as a frozen game, not as a two-player match.
    ///
    /// Uses FindObjectsInactive.Include so a house hidden by the previous match can be brought
    /// back for the next one.
    /// </summary>
    private void ApplyActivePlayerVisibility()
    {
        TokenMovement[] tokens = FindObjectsByType<TokenMovement>(FindObjectsInactive.Include);
        int hidden = 0;

        foreach (TokenMovement token in tokens)
        {
            bool playing = activePlayers.Contains(token.tokenColor);
            token.gameObject.SetActive(playing);
            if (!playing) hidden++;
        }

        Debug.Log($"[GameManager] {tokens.Length - hidden} tokens on the board, {hidden} hidden.");
    }

    /// <summary>
    /// Puts every token back in its yard at kickoff.
    ///
    /// Tokens were being authored into the scene already on the board — one Red token sat at tile
    /// 55, one step from finishing. That made "no tokens out, no six" rolls look broken: the rules
    /// correctly found a legal move for the hidden token, so the turn waited for a tap that the
    /// player had no way to make, the countdown kept running and the roll button stayed dead.
    /// Starting from a known state removes a whole class of that bug.
    /// </summary>
    public void ResetAllTokensToYard()
    {
        TokenMovement[] tokens = FindObjectsByType<TokenMovement>(FindObjectsInactive.Include);
        foreach (TokenMovement token in tokens)
        {
            token.ResetToYard();
        }

        Debug.Log($"[GameManager] Reset {tokens.Length} tokens to their yards.");
    }

    public void InitializeMatch(PlayerColor startingColor)
    {
        if (activePlayers == null || activePlayers.Count == 0)
        {
            activePlayers = new List<PlayerColor> { PlayerColor.RED, PlayerColor.GREEN, PlayerColor.YELLOW, PlayerColor.BLUE };
        }
        InitializeMatch(activePlayers, startingColor);
    }

    public void InitializeMatch(int colorIndex)
    {
        PlayerColor targetColor = (PlayerColor)Mathf.Clamp(colorIndex, 0, 3);
        InitializeMatch(targetColor);
    }

    public void InitializeMatch(string startingColorStr)
    {
        if (System.Enum.TryParse(startingColorStr, true, out PlayerColor parsedColor))
        {
            InitializeMatch(parsedColor);
        }
        else
        {
            InitializeMatch(PlayerColor.RED);
        }
    }

    #endregion

    #region Dice Rolling & Selection Logic

    public void OnRollButtonClicked()
    {
        // Phase is the single source of truth. hasRolled/isRolling are kept in sync for any
        // existing UI that reads them, but they no longer decide whether a roll is allowed.
        if (currentPhase != TurnPhase.AwaitingRoll) return;
        StartCoroutine(RollDiceRoutine());
    }

    private IEnumerator RollDiceRoutine()
    {
        int mySequence = turnSequence;

        isRolling = true;
        currentPhase = TurnPhase.Rolling;

        // The countdown must not eat the dice animation.
        if (UIManager.Instance != null) UIManager.Instance.PauseTurnTimer();

        try
        {
            ResetDiceSelection();

            dice1Value = Random.Range(1, 7);
            dice2Value = Random.Range(1, 7);

            // Ludo NX house rule: only a DOUBLE SIX earns another roll. Any other double is an
            // ordinary roll. Captures and home arrivals also grant one, awarded where they happen.
            bool isDoubleSix = (dice1Value == 6 && dice2Value == 6);

            if (isDoubleSix)
            {
                consecutiveDoublesCount++;
                Debug.Log($"DOUBLE SIX by {currentTurnColor}! Streak: {consecutiveDoublesCount}");
            }
            else
            {
                consecutiveDoublesCount = 0;
            }

            if (dice1Value == dice2Value && PointsManager.Instance != null)
            {
                PointsManager.Instance.OnDoublesRolled(currentTurnColor);
            }

            if (dice1Value == 6 || dice2Value == 6)
            {
                if (PointsManager.Instance != null) PointsManager.Instance.OnSixRolled(currentTurnColor);
            }

            // Three double-sixes in a row voids the whole roll, matching the official three-sixes
            // penalty. Nothing moves and the turn passes immediately.
            if (consecutiveDoublesCount >= 3)
            {
                Debug.Log($"3 CONSECUTIVE DOUBLE SIXES by {currentTurnColor}! Turn forfeited!");
                consecutiveDoublesCount = 0;
                ClearExtraTurn();
                hasRolled = true;

                if (UIManager.Instance != null)
                {
                    UIManager.Instance.UpdateTurnUI(currentTurnColor, "3 DOUBLE SIXES — TURN FORFEITED");
                }

                yield return new WaitForSeconds(1.2f);
                if (mySequence != turnSequence) yield break;

                ResetDiceState();
                AdvanceToNextPlayer();
                yield break;
            }

            if (isDoubleSix) GrantExtraTurn("DOUBLE SIX");

            if (AudioManager.Instance != null) AudioManager.Instance.PlayDiceRoll();

            if (physicsRoller != null)
            {
                yield return physicsRoller.AnimateSpinRoutine(dice1Value, dice2Value);
            }
            else
            {
                yield return new WaitForSeconds(0.8f);
            }

            // The turn changed underneath us while the dice were spinning (timeout, forced pass).
            // Writing hasRolled/phase now would strand the next player with a dead roll button.
            if (mySequence != turnSequence)
            {
                Debug.Log("[GameManager] Roll routine finished after the turn moved on — discarding.");
                yield break;
            }

            hasRolled = true;

            Debug.Log($"Rolled [{dice1Value}] and [{dice2Value}] for {currentTurnColor}");

            if (!PlayerHasAnyLegalMove(currentTurnColor, dice1Value, dice2Value))
            {
                Debug.Log($"{currentTurnColor} has no legal move — turn passes.");

                if (UIManager.Instance != null)
                {
                    UIManager.Instance.UpdateTurnUI(currentTurnColor, "NO MOVE — PASSING");
                }

                currentPhase = TurnPhase.Resolving;
                yield return new WaitForSeconds(1.0f);
                if (mySequence != turnSequence) yield break;

                // A roll that produced nothing never earns another turn, double six or not.
                ClearExtraTurn();
                ResetDiceState();
                AdvanceToNextPlayer();
            }
            else
            {
                currentPhase = TurnPhase.AwaitingSelection;

                if (UIManager.Instance != null)
                {
                    string baseMsg = isDoubleSix ? "DOUBLE SIX! SELECT A DIE" : "SELECT A DIE";
                    if (PlayerHasLegalCombinedMove(currentTurnColor))
                    {
                        baseMsg += " (OR TAP BOTH TO COMBINE)";
                    }
                    UIManager.Instance.UpdateTurnUI(currentTurnColor, baseMsg);
                    UIManager.Instance.ResumeTurnTimer();
                }
            }
        }
        finally
        {
            isRolling = false;
        }
    }

    /// <summary>
    /// Claimed by a double six, a capture, or a token reaching home. Multiple grants in one turn
    /// collapse to a single extra roll — you don't bank them.
    /// </summary>
    public void GrantExtraTurn(string reason)
    {
        grantExtraTurn = true;
        extraTurnReason = reason;
    }

    private void ClearExtraTurn()
    {
        grantExtraTurn = false;
        extraTurnReason = null;
    }

    /// <summary>Global animation lock — see IsAnyTokenMoving.</summary>
    public bool TryBeginTokenMove()
    {
        if (IsAnyTokenMoving) return false;
        IsAnyTokenMoving = true;
        currentPhase = TurnPhase.Moving;
        if (UIManager.Instance != null) UIManager.Instance.PauseTurnTimer();
        return true;
    }

    public void EndTokenMove()
    {
        IsAnyTokenMoving = false;
    }

    private bool PlayerHasAnyLegalMove(PlayerColor color, int d1, int d2)
    {
        TokenMovement[] allTokens = FindObjectsByType<TokenMovement>(FindObjectsInactive.Exclude);

        foreach (TokenMovement t in allTokens)
        {
            if (t.tokenColor != color || t.isFinished) continue;

            if (d1 > 0 && TokenMovement.CanTokenMove(t, d1)) return true;
            if (d2 > 0 && TokenMovement.CanTokenMove(t, d2)) return true;
            // A combined-sum move is only ever legal for a token already on the board
            // (exiting the yard requires a literal die face of 6, not an arithmetic sum).
            if (d1 > 0 && d2 > 0 && !t.isHome && TokenMovement.CanTokenMove(t, d1 + d2)) return true;
        }

        return false;
    }

    public bool PlayerHasLegalCombinedMove(PlayerColor color)
    {
        if (dice1Value <= 0 || dice2Value <= 0) return false;

        TokenMovement[] allTokens = FindObjectsByType<TokenMovement>(FindObjectsInactive.Exclude);
        foreach (TokenMovement t in allTokens)
        {
            if (t.tokenColor != color || t.isFinished || t.isHome) continue;
            if (TokenMovement.CanTokenMove(t, dice1Value + dice2Value)) return true;
        }

        return false;
    }

    public bool PlayerHasLegalMoveForSingleDie(PlayerColor color, int dieValue)
    {
        if (dieValue <= 0) return false;

        TokenMovement[] allTokens = FindObjectsByType<TokenMovement>(FindObjectsInactive.Exclude);

        foreach (TokenMovement t in allTokens)
        {
            if (t.tokenColor != color || t.isFinished) continue;

            if (TokenMovement.CanTokenMove(t, dieValue)) return true;
        }

        return false;
    }

    public void GrantCaptureBonusTurn()
    {
        GrantExtraTurn("CAPTURE");
        Debug.Log($"{currentTurnColor} earned a BONUS TURN for capturing an opponent token!");
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateTurnUI(currentTurnColor, "BONUS TURN (CAPTURE)");
        }
    }

    /// <summary>Third way to earn another roll: landing a token in the home triangle.</summary>
    public void GrantHomeArrivalBonusTurn()
    {
        GrantExtraTurn("TOKEN HOME");
        Debug.Log($"{currentTurnColor} earned a BONUS TURN for bringing a token home!");
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateTurnUI(currentTurnColor, "BONUS TURN (TOKEN HOME)");
        }
    }

    // Called when the player clicks a physical die in the tray
    public void ToggleDiceSelection(GameObject diceObject)
    {
        if (!hasRolled) return;

        // Tapping either die while combined cancels combine mode entirely.
        if (isDiceCombined)
        {
            isDice1Selected = false;
            isDice2Selected = false;
            isDiceCombined = false;
            selectedDiceValue = 0;
            RefreshDiceVisuals();
            return;
        }

        if (diceObject == dice1Object && dice1Value > 0)
        {
            if (isDice1Selected)
            {
                isDice1Selected = false;
                selectedDiceValue = 0;
            }
            else if (isDice2Selected && dice2Value > 0)
            {
                // Second die tapped while the first is already selected -> combine both.
                isDice1Selected = true;
                isDiceCombined = true;
                selectedDiceValue = dice1Value + dice2Value;
            }
            else
            {
                isDice1Selected = true;
                isDice2Selected = false;
                selectedDiceValue = dice1Value;
            }
        }
        else if (diceObject == dice2Object && dice2Value > 0)
        {
            if (isDice2Selected)
            {
                isDice2Selected = false;
                selectedDiceValue = 0;
            }
            else if (isDice1Selected && dice1Value > 0)
            {
                // Second die tapped while the first is already selected -> combine both.
                isDice2Selected = true;
                isDiceCombined = true;
                selectedDiceValue = dice1Value + dice2Value;
            }
            else
            {
                isDice2Selected = true;
                isDice1Selected = false;
                selectedDiceValue = dice2Value;
            }
        }

        RefreshDiceVisuals();
    }

    private void RefreshDiceVisuals()
    {
        if (dice1Object != null)
        {
            DiceRoll d1 = dice1Object.GetComponent<DiceRoll>();
            if (d1 != null) d1.UpdateVisualState();
        }
        if (dice2Object != null)
        {
            DiceRoll d2 = dice2Object.GetComponent<DiceRoll>();
            if (d2 != null) d2.UpdateVisualState();
        }
    }

    public int GetSelectedDiceValue()
    {
        return selectedDiceValue;
    }

    /// <summary>
    /// Selects both dice as a combined sum move on a single token. Used by the player's
    /// "tap both dice" gesture and by AutoPlayAI when only the combined move is legal.
    /// </summary>
    public void SelectCombinedDice()
    {
        if (dice1Value <= 0 || dice2Value <= 0) return;

        isDice1Selected = true;
        isDice2Selected = true;
        isDiceCombined = true;
        selectedDiceValue = dice1Value + dice2Value;
        RefreshDiceVisuals();
    }

    public void SelectDice(int diceNumber)
    {
        isDiceCombined = false;

        if (diceNumber == 1 && dice1Value > 0)
        {
            isDice1Selected = true;
            isDice2Selected = false;
            selectedDiceValue = dice1Value;
        }
        else if (diceNumber == 2 && dice2Value > 0)
        {
            isDice1Selected = false;
            isDice2Selected = true;
            selectedDiceValue = dice2Value;
        }
        RefreshDiceVisuals();
    }

    public void ConsumeSelectedDice()
    {
        if (isDiceCombined)
        {
            // Both dice were spent together on a single sum-move — consume atomically.
            dice1Value = 0;
            dice2Value = 0;
            isDice1Selected = false;
            isDice2Selected = false;
            isDiceCombined = false;
            selectedDiceValue = 0;
            RefreshDiceVisuals();
            ResetTurnForNextPlayer();
            Debug.Log("Combined dice consumed successfully.");
            return;
        }

        // Use the isDiceXSelected flags as primary discriminator.
        // This prevents the wrong die being consumed when both dice
        // show the same value (doubles).
        if (isDice1Selected)
        {
            dice1Value = 0;
            isDice1Selected = false;
        }
        else if (isDice2Selected)
        {
            dice2Value = 0;
            isDice2Selected = false;
        }

        selectedDiceValue = 0;
        RefreshDiceVisuals();

        if (dice1Value == 0 && dice2Value == 0)
        {
            ResetTurnForNextPlayer();
        }
        else
        {
            int remainingDie = (dice1Value > 0) ? dice1Value : dice2Value;
            if (!PlayerHasLegalMoveForSingleDie(currentTurnColor, remainingDie))
            {
                Debug.Log($"{currentTurnColor} cannot use remaining die [{remainingDie}] — forfeiting remaining die.");
                dice1Value = 0;
                dice2Value = 0;
                RefreshDiceVisuals();
                ResetTurnForNextPlayer();
            }
            else
            {
                // Still the same player's turn with one die left — hand control back and restart
                // the clock, which previously died here and left the second die untimed.
                currentPhase = TurnPhase.AwaitingSelection;
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.UpdateTurnUI(currentTurnColor, $"MOVE AGAIN — DIE [{remainingDie}]");
                    UIManager.Instance.StartTurnTimer();
                }
            }
        }

        Debug.Log("Selected dice consumed successfully.");
    }

    public void ResetDiceSelection()
    {
        isDice1Selected = false;
        isDice2Selected = false;
        isDiceCombined = false;
        selectedDiceValue = 0;
    }

    public void ResetDiceState()
    {
        hasRolled = false;
        isRolling = false;
        dice1Value = 0;
        dice2Value = 0;
        ResetDiceSelection();
    }

    public void ResetTurnForNextPlayer()
    {
        string reason = extraTurnReason;
        bool keepTurn = grantExtraTurn;

        ResetDiceState();
        ClearExtraTurn();

        if (!keepTurn)
        {
            AdvanceToNextPlayer();
            return;
        }

        // Same player rolls again. This is a fresh decision point, so it needs a fresh sequence
        // (to invalidate anything still in flight) and a fresh clock.
        turnSequence++;
        currentPhase = TurnPhase.AwaitingRoll;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateTurnUI(currentTurnColor, $"ROLL AGAIN — {reason}");
            UIManager.Instance.StartTurnTimer();
        }
    }

    public void AdvanceToNextPlayer()
    {
        if (activePlayers == null || activePlayers.Count == 0) return;

        consecutiveDoublesCount = 0;
        ClearExtraTurn();

        // Invalidate every coroutine that belonged to the turn we're leaving.
        turnSequence++;
        currentPhase = TurnPhase.AwaitingRoll;
        IsAnyTokenMoving = false;
        hasRolled = false;

        // Skip players who have already finished all their tokens
        int attempts = 0;
        int currentIndex = activePlayers.IndexOf(currentTurnColor);
        do
        {
            currentIndex = (currentIndex + 1) % activePlayers.Count;
            attempts++;
        }
        while (leaderBoard.Contains(activePlayers[currentIndex]) && attempts < activePlayers.Count);

        currentTurnColor = activePlayers[currentIndex];

        // Driven directly rather than only via UIManager: the background is a world-space effect
        // and shouldn't stop working just because a HUD reference is unassigned.
        if (BackgroundAmbience.Instance != null)
        {
            BackgroundAmbience.Instance.TransitionToPlayerColor(currentTurnColor);
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateTurnUI(currentTurnColor, "ROLL DICE");
            UIManager.Instance.StartTurnTimer();
        }
    }

    #endregion

    #region Win Tracking

    public void CheckForPlayerWin(PlayerColor color)
    {
        TokenMovement[] allTokens = FindObjectsByType<TokenMovement>(FindObjectsInactive.Exclude);
        int total = 0;
        int finished = 0;

        foreach (TokenMovement t in allTokens)
        {
            if (t.tokenColor == color)
            {
                total++;
                if (t.isFinished) finished++;
            }
        }

        if (total > 0 && finished == total && !leaderBoard.Contains(color))
        {
            leaderBoard.Add(color);
            Debug.Log($"{color} has finished all tokens! Placed #{leaderBoard.Count}");

            // Match end is a game fact, not a UI one — gate it on the board state alone so a scene
            // without a UIManager still awards placement bonuses and still ends cleanly.
            if (activePlayers.Count > 0 && leaderBoard.Count >= activePlayers.Count)
            {
                if (PointsManager.Instance != null)
                {
                    PointsManager.Instance.AwardPlacementBonuses(leaderBoard);
                }

                // When the menu is present it owns the post-match screen, because it also banks
                // coins and NX. Two victory screens fighting over the same moment is worse than
                // either one alone.
                if (LudoMenuFlow.Instance != null)
                {
                    LudoMenuFlow.NotifyMatchEnded(leaderBoard);
                }
                else if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowGameOverUI(leaderBoard);
                }
            }
        }
    }

    #endregion
}