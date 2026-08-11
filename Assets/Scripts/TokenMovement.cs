using UnityEngine;
using System.Collections;

public class TokenMovement : MonoBehaviour
{
    public GameManager.PlayerColor tokenColor;
    public bool isHome = true;
    public int currentTileIndex = 0;
    public int stepsTaken = 0;

    public bool isFinished = false;
    public bool isInHomeBase => isHome;

    [Header("Movement References")]
    public LudoPath pathScript;

    [Header("Assign this token's empty home-base slot (for capture reset)")]
    public Transform homeBaseSlot;

    [Header("Animation Settings")]
    public float hopDuration = 0.15f;
    public float hopHeight = 0.35f;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool isHighlighted = false;
    private bool isMoving = false;

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
        if (isMoving) return;

        int selectedValue = GameManager.Instance.GetSelectedDiceValue();
        if (selectedValue <= 0)
        {
            Debug.LogWarning("Select a valid dice first!");
            return;
        }

        if (!CanTokenMove(this, selectedValue))
        {
            Debug.LogWarning($"{tokenColor} token cannot make this move (overshoot, blocked by enemy blockade, or invalid roll)!");
            return;
        }

        if (isHome)
        {
            StartCoroutine(AnimateMoveOutOfHome());
        }
        else
        {
            StartCoroutine(AnimateAdvanceToken(selectedValue));
        }
    }

    public void MoveSteps(int steps)
    {
        if (isFinished) return;
        if (isMoving) return;

        if (isHome)
        {
            if (steps == 6)
            {
                StartCoroutine(AnimateMoveOutOfHome());
            }
        }
        else
        {
            StartCoroutine(AnimateAdvanceToken(steps));
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

    #region Animated Movement

    private IEnumerator AnimateMoveOutOfHome()
    {
        isMoving = true;

        isHome = false;
        currentTileIndex = 0;
        stepsTaken = 1;

        // Animate hop from home base to start tile
        Transform targetTile = GetCurrentTileTransform();
        if (targetTile != null)
        {
            yield return StartCoroutine(HopToPosition(targetTile.position));
        }
        else
        {
            UpdatePhysicalPosition();
        }

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySpawn();
        CheckForCaptures();

        Debug.Log($"{tokenColor} Token moved OUT of home base!");

        isMoving = false;
        GameManager.Instance.ConsumeSelectedDice();
    }

    private IEnumerator AnimateAdvanceToken(int steps)
    {
        if (pathScript == null)
        {
            pathScript = Object.FindAnyObjectByType<LudoPath>();
            if (pathScript == null) yield break;
        }

        int pathLength = pathScript.GetPathLength(tokenColor);
        int newIndex = currentTileIndex + steps;

        if (!CanTokenMove(this, steps))
        {
            Debug.Log($"{tokenColor} token move blocked (overshoot or enemy blockade).");
            yield break;
        }

        isMoving = true;

        int startIndex = currentTileIndex;

        // Hop tile-by-tile so the player can see each step
        for (int i = 1; i <= steps; i++)
        {
            int intermediateIndex = startIndex + i;
            Transform tileTf = pathScript.GetTileTransform(tokenColor, intermediateIndex);

            if (tileTf != null)
            {
                yield return StartCoroutine(HopToPosition(tileTf.position));
            }
        }

        currentTileIndex = newIndex;
        stepsTaken += steps;

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

        isMoving = false;
        GameManager.Instance.ConsumeSelectedDice();
    }

    /// <summary>
    /// Smoothly hops the token to a target position with a small arc.
    /// </summary>
    private IEnumerator HopToPosition(Vector3 target)
    {
        Vector3 start = transform.position;
        // Keep the token's Z so it stays in front of the board
        target.z = start.z;

        float elapsed = 0f;

        while (elapsed < hopDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / hopDuration);

            // Smooth ease-in-out
            float smooth = t * t * (3f - 2f * t);

            // Parabolic arc for the hop (peaks at t=0.5)
            float arc = hopHeight * 4f * t * (1f - t);

            Vector3 pos = Vector3.Lerp(start, target, smooth);
            pos.y += arc;

            transform.position = pos;
            yield return null;
        }

        transform.position = target;
    }

    #endregion

    #region Captures & Positioning

    private void CheckForCaptures()
    {
        if (pathScript == null) return;

        int myAbsolute = pathScript.GetAbsoluteMainIndex(tokenColor, currentTileIndex);
        if (myAbsolute < 0) return;

        if (pathScript.IsAbsoluteTileSafe(myAbsolute)) return;

        TokenMovement[] allTokens = Object.FindObjectsByType<TokenMovement>(FindObjectsInactive.Exclude);

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
                if (GameManager.Instance != null) GameManager.Instance.GrantCaptureBonusTurn();
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

    private Transform GetCurrentTileTransform()
    {
        if (pathScript == null)
        {
            pathScript = Object.FindAnyObjectByType<LudoPath>();
        }
        if (pathScript != null)
        {
            return pathScript.GetTileTransform(tokenColor, currentTileIndex);
        }
        return null;
    }

    private void UpdatePhysicalPosition()
    {
        Transform targetTile = GetCurrentTileTransform();
        if (targetTile != null)
        {
            transform.position = targetTile.position;
        }
    }

    #endregion

    #region Legal Move & Blockade Rules

    /// <summary>
    /// Checks if a token can legally move the specified steps, adhering to exact finish rules and enemy blockades.
    /// </summary>
    public static bool CanTokenMove(TokenMovement token, int steps)
    {
        if (token == null || token.isFinished || steps <= 0) return false;

        if (token.isHome)
        {
            if (steps != 6) return false;
            // Check if start tile is blockaded by enemy
            if (token.pathScript != null)
            {
                int startAbs = token.pathScript.GetAbsoluteMainIndex(token.tokenColor, 0);
                if (startAbs >= 0 && IsTileBlockadedByEnemy(startAbs, token.tokenColor))
                    return false;
            }
            return true;
        }

        if (token.pathScript == null)
        {
            token.pathScript = Object.FindAnyObjectByType<LudoPath>();
            if (token.pathScript == null) return false;
        }

        int pathLength = token.pathScript.GetPathLength(token.tokenColor);
        if (token.currentTileIndex + steps > pathLength - 1) return false; // Exact finish / overshoot check

        // Check each step along the path for an enemy blockade (2+ enemy tokens on same tile)
        for (int stepOffset = 1; stepOffset <= steps; stepOffset++)
        {
            int checkRelIndex = token.currentTileIndex + stepOffset;
            int checkAbsIndex = token.pathScript.GetAbsoluteMainIndex(token.tokenColor, checkRelIndex);
            if (checkAbsIndex >= 0 && IsTileBlockadedByEnemy(checkAbsIndex, token.tokenColor))
            {
                return false; // Path or destination blocked by an enemy blockade
            }
        }

        return true;
    }

    /// <summary>
    /// Returns true if an absolute main-loop tile is blockaded by 2+ enemy tokens of the same color.
    /// </summary>
    public static bool IsTileBlockadedByEnemy(int absoluteTileIndex, GameManager.PlayerColor myColor)
    {
        if (absoluteTileIndex < 0) return false;

        TokenMovement[] allTokens = Object.FindObjectsByType<TokenMovement>(FindObjectsInactive.Exclude);
        System.Collections.Generic.Dictionary<GameManager.PlayerColor, int> enemyCounts =
            new System.Collections.Generic.Dictionary<GameManager.PlayerColor, int>();

        foreach (TokenMovement t in allTokens)
        {
            if (t.isHome || t.isFinished || t.tokenColor == myColor) continue;
            if (t.pathScript == null) continue;

            int tAbs = t.pathScript.GetAbsoluteMainIndex(t.tokenColor, t.currentTileIndex);
            if (tAbs == absoluteTileIndex)
            {
                if (!enemyCounts.ContainsKey(t.tokenColor)) enemyCounts[t.tokenColor] = 0;
                enemyCounts[t.tokenColor]++;

                if (enemyCounts[t.tokenColor] >= 2) return true; // Blockade formed by 2+ enemy tokens!
            }
        }

        return false;
    }

    #endregion
}