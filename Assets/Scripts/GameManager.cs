using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum PlayerColor { RED, GREEN, YELLOW, BLUE }

    [Header("Match Setup")]
    [Range(2, 4)]
    public int totalPlayers = 4;
    public List<PlayerColor> activePlayers = new List<PlayerColor>();

    [Header("Turn Tracking")]
    public PlayerColor currentTurn = PlayerColor.RED;
    public bool hasBonusRoll = false;

    [Header("Rules & Ranking Settings")]
    public int consecutiveSixesCount = 0;
    public bool isGameOver = false;

    public List<PlayerColor> leaderBoard = new List<PlayerColor>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InitializeMatch(totalPlayers);
    }

    public void InitializeMatch(int numberOfPlayers)
    {
        totalPlayers = Mathf.Clamp(numberOfPlayers, 2, 4);
        activePlayers.Clear();
        leaderBoard.Clear();
        isGameOver = false;

        switch (totalPlayers)
        {
            case 2:
                activePlayers.Add(PlayerColor.RED);
                activePlayers.Add(PlayerColor.YELLOW);
                break;

            case 3:
                activePlayers.Add(PlayerColor.RED);
                activePlayers.Add(PlayerColor.GREEN);
                activePlayers.Add(PlayerColor.YELLOW);
                break;

            case 4:
            default:
                activePlayers.Add(PlayerColor.RED);
                activePlayers.Add(PlayerColor.GREEN);
                activePlayers.Add(PlayerColor.YELLOW);
                activePlayers.Add(PlayerColor.BLUE);
                break;
        }

        currentTurn = activePlayers[0];
        ConfigureSceneTokens();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateTurnUI(currentTurn, "ROLL DICE");
        }

        Debug.Log("🎮 MATCH INITIALIZED FOR " + totalPlayers + " PLAYERS.");
        Debug.Log("--- FIRST TURN: " + currentTurn + " ---");
    }

    private void ConfigureSceneTokens()
    {
        TokenMovement[] allTokens = Object.FindObjectsByType<TokenMovement>(FindObjectsInactive.Include);

        foreach (TokenMovement token in allTokens)
        {
            bool isActiveColor = activePlayers.Contains(token.tokenColor);
            token.gameObject.SetActive(isActiveColor);

            if (isActiveColor)
            {
                token.ResetToHome();
            }
        }
    }

    public bool IsMyTurn(PlayerColor color)
    {
        return !isGameOver && (color == currentTurn) && !HasPlayerFinished(color);
    }

    public void RegisterRoll(int rollValue)
    {
        if (rollValue == 6)
        {
            consecutiveSixesCount++;
            Debug.Log(currentTurn + " rolled a 6! Consecutive 6s: " + consecutiveSixesCount);

            if (consecutiveSixesCount >= 3)
            {
                Debug.Log("⚠️ 3 SIXES IN A ROW! Turn forfeited for " + currentTurn);
                hasBonusRoll = false;
                consecutiveSixesCount = 0;
                DiceRoll.lastRolledValue = 0;
                NextTurn();
                return;
            }
        }
        else
        {
            consecutiveSixesCount = 0;
        }
    }

    public void ProcessRollResult(int rollValue)
    {
        RegisterRoll(rollValue);

        if (DiceRoll.lastRolledValue == 0) return;

        if (!HasValidMove(rollValue))
        {
            Debug.Log("⚠️ No valid moves for " + currentTurn + " with roll " + rollValue + ". Auto-skipping turn!");
            
            DiceRoll.lastRolledValue = 0;
            hasBonusRoll = false;
            NextTurn();
        }
    }

    public bool HasValidMove(int diceValue)
    {
        TokenMovement[] allTokens = Object.FindObjectsByType<TokenMovement>(FindObjectsInactive.Exclude);
        int totalPathLength = 56;

        foreach (TokenMovement token in allTokens)
        {
            if (token.tokenColor == currentTurn && !token.isFinished)
            {
                if (token.isInHomeBase && diceValue == 6)
                {
                    return true;
                }

                if (!token.isInHomeBase && (token.stepsTaken + diceValue <= totalPathLength))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public void NextTurn()
    {
        if (isGameOver) return;

        CheckCurrentPlayerCompletion();

        if (isGameOver) return;

        // Recalculate Live Win Percentages
        if (WinCalculator.Instance != null)
        {
            WinCalculator.Instance.CalculateLiveWinPercentages();
        }

        if (hasBonusRoll && !HasPlayerFinished(currentTurn))
        {
            Debug.Log("🎲 BONUS ROLL for " + currentTurn + "!");
            hasBonusRoll = false;

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateTurnUI(currentTurn, "BONUS ROLL!");
            }
        }
        else
        {
            hasBonusRoll = false;
            consecutiveSixesCount = 0;
            
            AdvanceToNextActivePlayer();
            Debug.Log("--- NEXT TURN: " + currentTurn + " ---");

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateTurnUI(currentTurn, "ROLL DICE");
            }
        }
    }

    private void AdvanceToNextActivePlayer()
    {
        if (activePlayers.Count == 0) return;

        int currentIndex = activePlayers.IndexOf(currentTurn);
        int safetyCounter = 0;

        do
        {
            currentIndex = (currentIndex + 1) % activePlayers.Count;
            currentTurn = activePlayers[currentIndex];
            safetyCounter++;
        } 
        while (HasPlayerFinished(currentTurn) && safetyCounter < activePlayers.Count);
    }

    private bool HasPlayerFinished(PlayerColor color)
    {
        return leaderBoard.Contains(color);
    }

    public void CheckCurrentPlayerCompletion()
    {
        if (HasPlayerFinished(currentTurn)) return;

        TokenMovement[] allTokens = Object.FindObjectsByType<TokenMovement>(FindObjectsInactive.Exclude);
        int finishedTokensCount = 0;

        foreach (TokenMovement token in allTokens)
        {
            if (token.tokenColor == currentTurn && token.isFinished)
            {
                finishedTokensCount++;
            }
        }

        if (finishedTokensCount == 4)
        {
            leaderBoard.Add(currentTurn);
            int rank = leaderBoard.Count;

            string positionSuffix = rank == 1 ? "1st PLACE 🏆" : (rank == 2 ? "2nd PLACE 🥈" : "3rd PLACE 🥉");
            Debug.Log("🎉 " + currentTurn + " HAS FINISHED ALL TOKENS AND TAKES " + positionSuffix + "! 🎉");

            if (leaderBoard.Count >= activePlayers.Count - 1)
            {
                isGameOver = true;

                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowGameOverUI(leaderBoard);
                }

                Debug.Log("🏁 GAME OVER! FINAL LEADERBOARD:");
                for (int i = 0; i < leaderBoard.Count; i++)
                {
                    Debug.Log((i + 1) + " Position: " + leaderBoard[i]);
                }
            }
        }
    }
}