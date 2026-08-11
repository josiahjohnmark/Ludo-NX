using UnityEngine;
using System.Collections;

public class TokenMovement : MonoBehaviour
{
    public GameManager.PlayerColor tokenColor;
    public bool isHome = true;
    public int currentTileIndex = -1;
    public int stepsTaken = 0;

    public bool isFinished = false;
    public bool isInHomeBase => isHome;

    [Header("Movement References")]
    public LudoPath pathScript;

    [Header("Assign this token's empty home-base slot (for capture reset)")]
    public Transform homeBaseSlot;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool isHighlighted = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        if (pathScript == null)
        {
            pathScript = Object.FindAnyObjectByType<LudoPath>();
        }
    }

    private void OnMouseDown()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.currentTurnColor != tokenColor) return;
        if (isFinished) return;

        int selectedValue = GameManager.Instance.GetSelectedDiceValue();
        if (selectedValue <= 0)
        {
            Debug.LogWarning("Select a valid dice first!");
            return;
        }

        if (isHome)
        {
            if (selectedValue == 6)
            {
                MoveOutOfHome();
                GameManager.Instance.ConsumeSelectedDice();
            }
            else
            {
                Debug.Log("You need a 6 to bring this token out!");
            }
        }
        else
        {
            bool moved = AdvanceToken(selectedValue);
            if (moved)
            {
                GameManager.Instance.ConsumeSelectedDice();
            }
        }
    }

    public void MoveSteps(int steps)
    {
        if (isFinished) return;

        if (isHome)
        {
            if (steps == 6)
            {
                MoveOutOfHome();
            }
        }
        else
        {
            AdvanceToken(steps);
        }
    }

    public void SetHighlight(bool enable)
    {
        isHighlighted = enable;
        if (spriteRenderer == null) return;

        if (enable)
        {
            StopAllCoroutines();
            StartCoroutine(PulseHighlight());
        }
        else
        {
            StopAllCoroutines();
            spriteRenderer.color = originalColor;
        }
    }

    private IEnumerator PulseHighlight()
    {
        while (isHighlighted)
        {
            float pingPong = Mathf.PingPong(Time.time * 4f, 0.5f) + 0.5f;
            spriteRenderer.color = new Color(originalColor.r * pingPong, originalColor.g * pingPong, originalColor.b * pingPong, originalColor.a);
            yield return null;
        }
    }

    private void MoveOutOfHome()
    {
        isHome = false;
        currentTileIndex = 0;
        stepsTaken = 1;

        UpdatePhysicalPosition();

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySpawn();

        CheckForCaptures();

        Debug.Log($"{tokenColor} Token moved OUT of home base!");
    }

    private bool AdvanceToken(int steps)
    {
        if (pathScript == null)
        {
            pathScript = Object.FindAnyObjectByType<LudoPath>();
            if (pathScript == null) return false;
        }

        int pathLength = pathScript.GetPathLength(tokenColor);
        int newIndex = currentTileIndex + steps;

        if (newIndex > pathLength - 1)
        {
            Debug.Log($"{tokenColor} token needs an exact roll to finish — move blocked (overshoot).");
            return false;
        }

        currentTileIndex = newIndex;
        stepsTaken += steps;

        UpdatePhysicalPosition();

        int mainLoopLength = pathLength - 6;

        if (currentTileIndex == pathLength - 1)
        {
            isFinished = true;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayVictory();
            if (GameManager.Instance != null) GameManager.Instance.CheckForPlayerWin(tokenColor);
            Debug.Log($"{tokenColor} token FINISHED!");
        }
        else if (currentTileIndex < mainLoopLength)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayHop();
            CheckForCaptures();
        }
        else
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayHomeStretchHop(currentTileIndex - mainLoopLength, 6);
        }

        Debug.Log($"{tokenColor} Token moved {steps} steps to tile {currentTileIndex} (Total steps: {stepsTaken})");
        return true;
    }

    private void CheckForCaptures()
    {
        if (pathScript == null) return;

        int myAbsolute = pathScript.GetAbsoluteMainIndex(tokenColor, currentTileIndex);
        if (myAbsolute < 0) return;

        if (pathScript.IsAbsoluteTileSafe(myAbsolute)) return;

        TokenMovement[] allTokens = Object.FindObjectsByType<TokenMovement>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (TokenMovement other in allTokens)
        {
            if (other == this) continue;
            if (other.tokenColor == tokenColor) continue;
            if (other.isHome || other.isFinished) continue;

            int otherAbsolute = pathScript.GetAbsoluteMainIndex(other.tokenColor, other.currentTileIndex);
            if (otherAbsolute < 0) continue;

            if (otherAbsolute == myAbsolute)
            {
                other.SendHome();
                if (AudioManager.Instance != null) AudioManager.Instance.PlayCapture();
                Debug.Log($"{tokenColor} captured {other.tokenColor}'s token!");
            }
        }
    }

    public void SendHome()
    {
        isHome = true;
        currentTileIndex = -1;
        stepsTaken = 0;

        if (homeBaseSlot != null)
        {
            transform.position = homeBaseSlot.position;
        }
    }

    private void UpdatePhysicalPosition()
    {
        if (pathScript == null)
        {
            pathScript = Object.FindAnyObjectByType<LudoPath>();
        }

        if (pathScript != null)
        {
            Transform targetTile = pathScript.GetTileTransform(tokenColor, currentTileIndex);
            if (targetTile != null)
            {
                transform.position = targetTile.position;
            }
        }
    }
}