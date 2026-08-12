using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
    public float hopDuration = 0.32f;
    public float hopHeight = 0.35f;
    // How much the token grows at the apex of a hop. In a 2D top-down board this scale change is
    // what actually reads as "lifted off the board" — a Y offset alone just reads as moving to a
    // different tile.
    public float hopScalePunch = 0.12f;
    public float penultimateRampHeight = 0.45f;
    public float captureSlamHeight = 0.6f;

    [Header("Landing Squash")]
    public float landingSquashDuration = 0.09f;
    public float landingSquashAmount = 0.12f;

    [Header("Sorting")]
    // Added to the token's sorting order while airborne so it passes over other tokens.
    public int airborneSortingBoost = 20;

    [Header("Flow")]
    // 0 = perfectly even tile rhythm, 1 = full ease-in-out across the whole move. A small amount
    // softens the start and stop without making the middle hops noticeably uneven.
    [Range(0f, 1f)] public float flowEasing = 0.35f;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Vector3 originalScale = Vector3.one;
    private int baseSortingOrder;
    private bool isHighlighted = false;
    private bool isMoving = false;
    private Coroutine highlightCoroutine;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            baseSortingOrder = spriteRenderer.sortingOrder;
        }

        originalScale = transform.localScale;

        if (pathScript == null)
        {
            pathScript = LudoPath.FindComplete();
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

        if (isHome && GameManager.Instance.isDiceCombined)
        {
            Debug.LogWarning("Cannot exit the yard with combined dice — you need a single die showing 6.");
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

        // Scoped to just the pulse coroutine — StopAllCoroutines() here would also kill an
        // in-flight hop, stranding the token mid-air with isMoving stuck true.
        if (highlightCoroutine != null)
        {
            StopCoroutine(highlightCoroutine);
            highlightCoroutine = null;
        }

        if (enable)
        {
            highlightCoroutine = StartCoroutine(PulseHighlight());
        }
        else
        {
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

        // Animate hop from home base to start tile. The start tile is always a safe zone,
        // so this landing is never a capture.
        Transform targetTile = GetCurrentTileTransform();
        if (targetTile != null)
        {
            yield return StartCoroutine(AnimateHopSequence(new List<Vector3> { targetTile.position }, false));
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
            pathScript = LudoPath.FindComplete();
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

        // Collect every tile this move crosses, then animate them as one continuous sequence.
        List<Vector3> waypoints = new List<Vector3>();
        for (int i = 1; i <= steps; i++)
        {
            Transform tileTf = pathScript.GetTileTransform(tokenColor, startIndex + i);
            if (tileTf != null) waypoints.Add(tileTf.position);
        }

        int finalAbsIndex = pathScript.GetAbsoluteMainIndex(tokenColor, newIndex);
        bool landingIsCapture = finalAbsIndex >= 0
            && !pathScript.IsAbsoluteTileSafe(finalAbsIndex)
            && IsEnemyPresentOnAbsoluteTile(finalAbsIndex);

        if (waypoints.Count > 0)
        {
            yield return StartCoroutine(AnimateHopSequence(waypoints, landingIsCapture));
        }

        currentTileIndex = newIndex;
        stepsTaken += steps;

        int mainLoopLength = pathLength - 6;

        if (currentTileIndex == pathLength - 1)
        {
            isFinished = true;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayVictory();
            if (PointsManager.Instance != null)
            {
                PointsManager.Instance.OnTokenReachedHome(tokenColor);
            }
            if (FloatingTextFX.Instance != null)
            {
                FloatingTextFX.Instance.SpawnFloatingText(transform.position, 5);
            }
            if (GameManager.Instance != null) GameManager.Instance.CheckForPlayerWin(tokenColor);
            Debug.Log($"{tokenColor} token FINISHED!");
        }
        else if (currentTileIndex < mainLoopLength)
        {
            // Hop ticks already fired per-tile inside AnimateHopSequence.
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
    /// Animates a whole multi-tile move as ONE continuous timeline rather than a chain of
    /// separate per-tile coroutines.
    ///
    /// Horizontal motion is a straight lerp along the tile-to-tile polyline (so it can never
    /// swing sideways at a corner) advancing at a near-constant rate — there is a single
    /// acceleration at the start and a single deceleration at the end, instead of the token
    /// braking to a dead stop on every tile, which is what made the old version stutter.
    ///
    /// The raise-and-drop comes from a full sine hump per tile: sin() departs and lands steeply
    /// and hangs at the apex, which reads as a real hop. A synchronised scale punch sells the
    /// height in 2D, where a Y offset alone just looks like sliding further up the board.
    /// </summary>
    private IEnumerator AnimateHopSequence(List<Vector3> waypoints, bool landingIsCapture)
    {
        if (waypoints == null || waypoints.Count == 0) yield break;

        int tileCount = waypoints.Count;
        Vector3 start = transform.position;
        float z = start.z;

        Vector3[] points = new Vector3[tileCount + 1];
        points[0] = start;
        for (int i = 0; i < tileCount; i++)
        {
            Vector3 p = waypoints[i];
            p.z = z;
            points[i + 1] = p;
        }

        // Per-tile hop heights. Ordinary moves stay at a steady height; only a run that actually
        // ends in a capture ramps up, so the escalation reads as intent rather than randomness.
        float[] heights = new float[tileCount];
        for (int i = 0; i < tileCount; i++)
        {
            if (!landingIsCapture) heights[i] = hopHeight;
            else if (i == tileCount - 1) heights[i] = captureSlamHeight;
            else if (i == tileCount - 2) heights[i] = penultimateRampHeight;
            else heights[i] = hopHeight;
        }

        float totalDuration = hopDuration * tileCount;
        if (landingIsCapture) totalDuration += hopDuration * 0.5f;  // let the slam land heavier

        if (spriteRenderer != null) spriteRenderer.sortingOrder = baseSortingOrder + airborneSortingBoost;

        int lastSoundedTile = -1;
        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;
            float rawT = Mathf.Clamp01(elapsed / totalDuration);

            // One gentle ease across the ENTIRE move, not per tile.
            float easedT = Mathf.Lerp(rawT, Mathf.SmoothStep(0f, 1f, rawT), flowEasing);

            float travel = easedT * tileCount;
            int seg = Mathf.Clamp(Mathf.FloorToInt(travel), 0, tileCount - 1);
            float segT = Mathf.Clamp01(travel - seg);

            Vector3 groundPos = Vector3.Lerp(points[seg], points[seg + 1], segT);
            float arc = Mathf.Sin(segT * Mathf.PI);

            transform.position = groundPos + Vector3.up * (arc * heights[seg]);
            transform.localScale = originalScale * (1f + arc * hopScalePunch);

            if (seg != lastSoundedTile)
            {
                lastSoundedTile = seg;
                PlayHopSound(seg, tileCount);
            }

            yield return null;
        }

        transform.position = points[tileCount];
        transform.localScale = originalScale;

        if (spriteRenderer != null) spriteRenderer.sortingOrder = baseSortingOrder;

        yield return StartCoroutine(LandingSquash());
    }

    /// <summary>
    /// A brief squash on arrival only. Squashing on every intermediate tile reads as noise.
    /// </summary>
    private IEnumerator LandingSquash()
    {
        if (landingSquashDuration <= 0f || landingSquashAmount <= 0f) yield break;

        float elapsed = 0f;
        while (elapsed < landingSquashDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / landingSquashDuration);

            // Squash hard on impact, then spring back out.
            float squash = Mathf.Sin(t * Mathf.PI) * landingSquashAmount;
            transform.localScale = new Vector3(
                originalScale.x * (1f + squash),
                originalScale.y * (1f - squash),
                originalScale.z);

            yield return null;
        }

        transform.localScale = originalScale;
    }

    /// <summary>
    /// One hop tick per tile, rising slightly in pitch across the move so a long run reads as a
    /// climbing run rather than a flat repeated click.
    /// </summary>
    private void PlayHopSound(int tileIndex, int tileCount)
    {
        if (AudioManager.Instance == null) return;

        float progress = tileCount > 1 ? (float)tileIndex / (tileCount - 1) : 0f;
        AudioManager.Instance.PlayHop(Mathf.Lerp(0.96f, 1.12f, progress));
    }

    /// <summary>
    /// True if a non-home, non-finished enemy token currently occupies this absolute tile.
    /// </summary>
    private bool IsEnemyPresentOnAbsoluteTile(int absoluteTileIndex)
    {
        if (absoluteTileIndex < 0) return false;

        TokenMovement[] allTokens = Object.FindObjectsByType<TokenMovement>(FindObjectsInactive.Exclude);

        foreach (TokenMovement other in allTokens)
        {
            if (other == this || other.tokenColor == tokenColor || other.isHome || other.isFinished) continue;
            if (other.pathScript == null) continue;

            int otherAbs = other.pathScript.GetAbsoluteMainIndex(other.tokenColor, other.currentTileIndex);
            if (otherAbs == absoluteTileIndex) return true;
        }

        return false;
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

                if (PointsManager.Instance != null)
                {
                    PointsManager.Instance.OnTokenCaptured(tokenColor, other.tokenColor);
                }

                if (FloatingTextFX.Instance != null)
                {
                    FloatingTextFX.Instance.SpawnFloatingText(transform.position, 3);
                    FloatingTextFX.Instance.SpawnFloatingText(other.transform.position, -1);
                }

                if (AudioManager.Instance != null) AudioManager.Instance.PlayCapture();
                if (GameManager.Instance != null) GameManager.Instance.GrantCaptureBonusTurn();

                StartCoroutine(CaptureCameraShake());
                Debug.Log($"{tokenColor} captured {other.tokenColor}'s token!");
            }
        }
    }

    private IEnumerator CaptureCameraShake()
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;

        Vector3 origPos = cam.transform.position;
        float duration = 0.15f;
        float elapsed = 0f;
        float magnitude = 0.08f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            cam.transform.position = new Vector3(origPos.x + x, origPos.y + y, origPos.z);
            yield return null;
        }

        cam.transform.position = origPos;
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
            pathScript = LudoPath.FindComplete();
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
            token.pathScript = LudoPath.FindComplete();
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