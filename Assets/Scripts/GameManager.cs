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
    public int selectedDiceValue = 0;

    [Header("Roll States & Doubles Penalty")]
    public bool isRolling = false;
    public bool hasRolled = false;
    public int consecutiveDoublesCount = 0;

    private bool grantExtraTurn = false;

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
        // Auto-initialize a 4-player match so the game is playable immediately.
        // Later this will be replaced by the player-setup UI flow.
        if (activePlayers == null || activePlayers.Count == 0)
        {
            InitializeMatchWithPlayerCount(4);
            Debug.Log("Auto-initialized 4-player match.");
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !isRolling && !hasRolled)
        {
            OnRollButtonClicked();
        }
    }

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

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateTurnUI(currentTurnColor, "ROLL DICE");
        }

        Debug.Log($"Match initialized. Players: {string.Join(", ", activePlayers)}. Starting: {currentTurnColor}");
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
        if (isRolling || hasRolled) return;
        StartCoroutine(RollDiceRoutine());
    }

    private IEnumerator RollDiceRoutine()
    {
        isRolling = true;
        try
        {
            ResetDiceSelection();

            dice1Value = Random.Range(1, 7);
            dice2Value = Random.Range(1, 7);

            bool isDouble = (dice1Value == dice2Value);

            if (isDouble)
            {
                consecutiveDoublesCount++;
                Debug.Log($"DOUBLES ROLLED [{dice1Value}-{dice2Value}] by {currentTurnColor}! Streak: {consecutiveDoublesCount}");
            }
            else
            {
                consecutiveDoublesCount = 0;
            }

            if (consecutiveDoublesCount >= 3)
            {
                Debug.Log($"3 CONSECUTIVE DOUBLES ROLLED by {currentTurnColor}! Turn forfeited!");
                consecutiveDoublesCount = 0;
                grantExtraTurn = false;
                hasRolled = true;

                if (UIManager.Instance != null)
                {
                    UIManager.Instance.UpdateTurnUI(currentTurnColor, "3 DOUBLES — TURN FORFEITED");
                }

                yield return new WaitForSeconds(1.2f);
                ResetDiceState();
                AdvanceToNextPlayer();
                yield break;
            }

            grantExtraTurn = isDouble;

            if (AudioManager.Instance != null) AudioManager.Instance.PlayDiceRoll();

            if (physicsRoller != null)
            {
                yield return physicsRoller.AnimateSpinRoutine(dice1Value, dice2Value);
            }
            else
            {
                yield return new WaitForSeconds(0.8f);
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

                yield return new WaitForSeconds(1.0f);

                ResetDiceState();
                AdvanceToNextPlayer();
            }
            else if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateTurnUI(currentTurnColor, isDouble ? "DOUBLES! SELECT A DIE" : "SELECT A DIE");
            }
        }
        finally
        {
            isRolling = false;
        }
    }

    private bool PlayerHasAnyLegalMove(PlayerColor color, int d1, int d2)
    {
        TokenMovement[] allTokens = FindObjectsByType<TokenMovement>(FindObjectsInactive.Exclude);

        foreach (TokenMovement t in allTokens)
        {
            if (t.tokenColor != color || t.isFinished) continue;

            if (d1 > 0 && TokenMovement.CanTokenMove(t, d1)) return true;
            if (d2 > 0 && TokenMovement.CanTokenMove(t, d2)) return true;
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
        grantExtraTurn = true;
        Debug.Log($"{currentTurnColor} earned a BONUS TURN for capturing an opponent token!");
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateTurnUI(currentTurnColor, "BONUS TURN (CAPTURE)");
        }
    }

    // Called when the player clicks a physical die in the tray
    public void ToggleDiceSelection(GameObject diceObject)
    {
        if (!hasRolled) return;

        if (diceObject == dice1Object && dice1Value > 0)
        {
            if (isDice1Selected)
            {
                isDice1Selected = false;
                selectedDiceValue = 0;
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

    public void SelectDice(int diceNumber)
    {
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
        }

        Debug.Log("Selected dice consumed successfully.");
    }

    public void ResetDiceSelection()
    {
        isDice1Selected = false;
        isDice2Selected = false;
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
        ResetDiceState();

        if (!grantExtraTurn)
        {
            AdvanceToNextPlayer();
        }
        else
        {
            if (UIManager.Instance != null)
                UIManager.Instance.UpdateTurnUI(currentTurnColor, "ROLL AGAIN (rolled a 6)");
        }

        grantExtraTurn = false;
    }

    public void AdvanceToNextPlayer()
    {
        if (activePlayers == null || activePlayers.Count == 0) return;

        consecutiveDoublesCount = 0;

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

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateTurnUI(currentTurnColor, "ROLL DICE");
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

            if (UIManager.Instance != null && activePlayers.Count > 0 && leaderBoard.Count >= activePlayers.Count)
            {
                UIManager.Instance.ShowGameOverUI(leaderBoard);
            }
        }
    }

    #endregion
}