using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Turn Indicator UI")]
    public TextMeshProUGUI turnStatusText;
    public Image turnBannerHeader;

    [Header("Player Colors")]
    public Color redColor = new Color(0.85f, 0.2f, 0.2f);
    public Color greenColor = new Color(0.2f, 0.75f, 0.3f);
    public Color yellowColor = new Color(0.95f, 0.8f, 0.2f);
    public Color blueColor = new Color(0.2f, 0.5f, 0.9f);

    [Header("Setup Menu Panel")]
    public GameObject playerSetupPanel;

    [Header("Victory / Leaderboard Panel")]
    public GameObject victoryPanel;
    public TextMeshProUGUI leaderboardText;

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

    private void Start()
    {
        if (victoryPanel != null) victoryPanel.SetActive(false);
    }

    public void UpdateTurnUI(GameManager.PlayerColor color, string actionMessage = "")
    {
        if (turnStatusText == null) return;

        Color bannerColor = GetColorForPlayer(color);

        if (turnBannerHeader != null)
        {
            turnBannerHeader.color = bannerColor;
        }

        if (string.IsNullOrEmpty(actionMessage))
        {
            turnStatusText.text = color.ToString() + "'S TURN";
        }
        else
        {
            turnStatusText.text = color.ToString() + ": " + actionMessage;
        }
    }

    public void ShowGameOverUI(List<GameManager.PlayerColor> leaderBoard)
    {
        if (victoryPanel == null) return;

        victoryPanel.SetActive(true);

        if (leaderboardText != null)
        {
            string summary = "FINAL PLACEMENTS\n\n";
            for (int i = 0; i < leaderBoard.Count; i++)
            {
                string rankSuffix = (i == 0) ? "1st Place 🏆" : ((i == 1) ? "2nd Place 🥈" : "3rd Place 🥉");
                summary += rankSuffix + ": " + leaderBoard[i].ToString() + "\n";
            }
            leaderboardText.text = summary;
        }
    }

    public void OnSelectPlayerCount(int playerCount)
    {
        if (playerSetupPanel != null) playerSetupPanel.SetActive(false);
        GameManager.Instance.InitializeMatch(playerCount);
        UpdateTurnUI(GameManager.Instance.currentTurn, "ROLL DICE");
    }

    public void OnClickRestartMatch()
    {
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (playerSetupPanel != null) playerSetupPanel.SetActive(true);
    }

    private Color GetColorForPlayer(GameManager.PlayerColor color)
    {
        return color switch
        {
            GameManager.PlayerColor.RED => redColor,
            GameManager.PlayerColor.GREEN => greenColor,
            GameManager.PlayerColor.YELLOW => yellowColor,
            GameManager.PlayerColor.BLUE => blueColor,
            _ => Color.white
        };
    }
}