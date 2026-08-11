using System;
using System.Collections.Generic;
using UnityEngine;

public class WinCalculator : MonoBehaviour
{
    public static WinCalculator Instance { get; private set; }

    [Header("Live UI Display Strings (e.g. 12.5%)")]
    public string redWinText = "0.0%";
    public string greenWinText = "0.0%";
    public string yellowWinText = "0.0%";
    public string blueWinText = "0.0%";

    [Header("Live Raw Percentages (Float Values)")]
    public float redWinPercentage;
    public float greenWinPercentage;
    public float yellowWinPercentage;
    public float blueWinPercentage;

    [Header("Debug Settings")]
    [SerializeField] private bool autoUpdateEveryFrame = true;

    // Core Constants: 100% / 4 tokens = 25% max per token
    private const float MAX_TOKEN_PERCENTAGE = 25f;
    private const float TOTAL_STEPS_TO_FINISH = 57f;

    // State Modifiers
    private const float ALL_FOUR_ACTIVE_BONUS = 3f;  // +3% if all 4 pieces are out on board
    private const float SAFE_LANE_BONUS = 2f;        // +2% per token inside safe home lane (steps 52+)

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (autoUpdateEveryFrame)
        {
            CalculateLiveWinPercentages();
        }
    }

    public Dictionary<GameManager.PlayerColor, float> CalculateLiveWinPercentages()
    {
        Dictionary<GameManager.PlayerColor, float> finalPercentages = new Dictionary<GameManager.PlayerColor, float>();

        // Explicitly use UnityEngine.Object to prevent ambiguous reference error
        TokenMovement[] allTokens = UnityEngine.Object.FindObjectsByType<TokenMovement>(FindObjectsInactive.Include);

        GameManager.PlayerColor[] colorsToTrack = new GameManager.PlayerColor[] 
        { 
            GameManager.PlayerColor.RED, 
            GameManager.PlayerColor.GREEN, 
            GameManager.PlayerColor.YELLOW, 
            GameManager.PlayerColor.BLUE 
        };

        foreach (GameManager.PlayerColor player in colorsToTrack)
        {
            float rawWinChance = 0f;
            int activeTokenCount = 0;
            bool hasTokensInScene = false;

            foreach (TokenMovement token in allTokens)
            {
                if (token.tokenColor == player)
                {
                    hasTokensInScene = true;

                    if (token.isFinished)
                    {
                        // Permanent 25.0%
                        rawWinChance += MAX_TOKEN_PERCENTAGE;
                    }
                    else if (!token.isInHomeBase)
                    {
                        activeTokenCount++;

                        // Core Step Calculation
                        float stepChance = (token.stepsTaken / TOTAL_STEPS_TO_FINISH) * MAX_TOKEN_PERCENTAGE;
                        rawWinChance += stepChance;

                        // Safe lane bonus (steps 52-57)
                        if (token.stepsTaken >= 52)
                        {
                            rawWinChance += SAFE_LANE_BONUS;
                        }
                    }
                }
            }

            if (hasTokensInScene)
            {
                // All 4 tokens active bonus
                if (activeTokenCount == 4)
                {
                    rawWinChance += ALL_FOUR_ACTIVE_BONUS;
                }

                // Clamp raw calculation between 0.0% and 99.0%
                float displayPercentage = Mathf.Clamp(rawWinChance, 0f, 99f);

                // Round to 1 decimal place
                displayPercentage = (float)Math.Round(displayPercentage, 1);

                // Full 100% check for winning leaderboard
                if (GameManager.Instance != null && GameManager.Instance.leaderBoard != null)
                {
                    if (GameManager.Instance.leaderBoard.Contains(player))
                    {
                        displayPercentage = 100f;
                    }
                }

                finalPercentages[player] = displayPercentage;
            }
            else
            {
                finalPercentages[player] = 0f;
            }
        }

        // Update Raw Float Values
        redWinPercentage = finalPercentages[GameManager.PlayerColor.RED];
        greenWinPercentage = finalPercentages[GameManager.PlayerColor.GREEN];
        yellowWinPercentage = finalPercentages[GameManager.PlayerColor.YELLOW];
        blueWinPercentage = finalPercentages[GameManager.PlayerColor.BLUE];

        // Format Strings with 1 Decimal Place and '%' Sign (e.g. "12.5%")
        redWinText = redWinPercentage.ToString("F1") + "%";
        greenWinText = greenWinPercentage.ToString("F1") + "%";
        yellowWinText = yellowWinPercentage.ToString("F1") + "%";
        blueWinText = blueWinPercentage.ToString("F1") + "%";

        return finalPercentages;
    }
}