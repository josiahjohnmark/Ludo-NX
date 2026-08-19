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

    [Header("Hop Timing")]
    // Per-tile hop duration, scaled down as the move gets longer. Ludo King sits around 0.13s per
    // tile, which reads as hurried; these are deliberately slower and weightier while still
    // capping a six-tile run so the player isn't left waiting.
    public float hopDurationShort = 0.42f;   // 1 tile
    public float hopDurationMedium = 0.34f;  // 2-3 tiles
    public float hopDurationLong = 0.26f;    // 4+ tiles
    public float maxTotalMoveDuration = 1.70f;

    [Header("Hop Shape")]
    public float hopHeight = 0.42f;
    // Scale growth at the apex. In a top-down 2D board a Y offset alone is ambiguous with "moved
    // further up the board", so the token also grows slightly as it rises.
    public float hopScalePunch = 0.085f;
    public float penultimateRampHeight = 0.52f;
    public float captureSlamHeight = 0.7f;
    public float captureHangTime = 0.18f;

    [Header("Ground Shadow")]
    // The single strongest height cue in 2D: a shadow that stays on the board while the token
    // leaves it. Generated at runtime, so it needs no imported art.
    public bool useGroundShadow = true;
    public float shadowBaseScale = 0.55f;

    [Tooltip("Minimum tap radius for a token, in world units. One board cell is about 0.46 " +
             "world units of radius, which is the practical ceiling before tokens steal each " +
             "other's taps.")]
    public float touchRadiusWorld = 0.45f;
    public float shadowApexScale = 0.62f;   // multiplier applied to base at full height
    public float shadowBaseAlpha = 0.45f;
    public float shadowApexAlpha = 0.18f;
    public Vector2 shadowOffset = new Vector2(0.02f, -0.06f);

    [Header("Landing Squash")]
    public float landingSquashDuration = 0.09f;
    public float landingSquashAmount = 0.12f;
    // Lighter squash applied on every intermediate landing, so a run reads as a series of real
    // impacts rather than one long glide with a bump at the end.
    public float perHopSquashAmount = 0.028f;

    [Header("Sorting")]
    // Added to the token's sorting order while airborne so it passes over other tokens.
    public int airborneSortingBoost = 20;

    [Header("Flow")]
    // Eases the HORIZONTAL advance only. Hop gravity is deliberately kept on its own constant
    // clock — driving the arc from eased progress stretched the first and last hops in time and
    // was the main source of the uneven rhythm.
    [Range(0f, 1f)] public float flowEasing = 0.75f;

    private Transform shadowTransform;
    private SpriteRenderer shadowRenderer;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Vector3 originalScale = Vector3.one;
    private int baseSortingOrder;
    private bool isHighlighted = false;
    private Coroutine highlightCoroutine;

    // ── Stack slot ──────────────────────────────────────────────────────────────────────────
    // Assigned by TokenStackLayout when this token shares a tile. Everything that positions or
    // scales the token at rest goes through RestingPosition/RestingScale rather than reading the
    // tile and originalScale directly, so a token in a stack cannot drift back on top of its
    // neighbours the next time it lands, squashes or is snapped.
    private Vector3 stackOffset = Vector3.zero;
    private float stackScale = 1f;
    private Coroutine settleCoroutine;

    /// <summary>True while this token is mid-hop and owns its own transform.</summary>
    public bool IsMoving { get; private set; }

    [Header("Stacking")]
    [Tooltip("How quickly a token slides to its place when a tile's stack changes.")]
    public float stackSettleDuration = 0.16f;

    /// <summary>Scale this token should rest at, accounting for how crowded its tile is.</summary>
    private Vector3 RestingScale => originalScale * stackScale;

    /// <summary>Radius one token occupies at full size — the footprint a whole stack must fit inside.</summary>
    public float StackFootprintRadius
    {
        get
        {
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                Vector3 ext = spriteRenderer.sprite.bounds.extents;
                return Mathf.Max(ext.x, ext.y) * Mathf.Max(originalScale.x, originalScale.y);
            }
            return 0.5f * Mathf.Max(originalScale.x, originalScale.y);
        }
    }

    /// <summary>The tile this token shares with others, or null when it is not on the path.</summary>
    public Transform GetStackTile()
    {
        return isHome ? null : GetCurrentTileTransform();
    }

    /// <summary>
    /// Takes a seat in its tile's stack. Animated by default so a token visibly shuffles aside to
    /// make room when a neighbour arrives, which reads as the stack reacting rather than as
    /// tokens teleporting.
    /// </summary>
    public void ApplyStackSlot(Vector3 offset, float scale, bool animate)
    {
        bool unchanged = (offset - stackOffset).sqrMagnitude < 0.000001f
                         && Mathf.Abs(scale - stackScale) < 0.0001f;

        stackOffset = offset;
        stackScale = scale;

        if (unchanged) return;

        // A token mid-hop drives its own transform; it picks the new slot up when it lands.
        if (IsMoving) return;

        if (settleCoroutine != null) StopCoroutine(settleCoroutine);

        if (!animate || !gameObject.activeInHierarchy)
        {
            ApplyRestingTransform();
            return;
        }

        settleCoroutine = StartCoroutine(SettleIntoSlot());
    }

    private void ApplyRestingTransform()
    {
        Transform tile = GetCurrentTileTransform();
        if (tile != null) transform.position = tile.position + stackOffset;
        else if (isHome && homeBaseSlot != null) transform.position = homeBaseSlot.position;

        transform.localScale = RestingScale;
    }

    private IEnumerator SettleIntoSlot()
    {
        Transform tile = GetCurrentTileTransform();
        Vector3 from = transform.position;
        Vector3 to = tile != null ? tile.position + stackOffset : from;
        Vector3 scaleFrom = transform.localScale;
        Vector3 scaleTo = RestingScale;

        float elapsed = 0f;
        while (elapsed < stackSettleDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / stackSettleDuration));
            transform.position = Vector3.Lerp(from, to, t);
            transform.localScale = Vector3.Lerp(scaleFrom, scaleTo, t);
            yield return null;
        }

        transform.position = to;
        transform.localScale = scaleTo;
        settleCoroutine = null;
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            baseSortingOrder = spriteRenderer.sortingOrder;
        }

        originalScale = transform.localScale;
        EnsureTouchTarget();

        if (pathScript == null)
        {
            pathScript = LudoPath.FindComplete();
        }
    }

    /// <summary>
    /// Grows the pick collider to at least cover the token you can see.
    ///
    /// The authored CircleCollider2D was 0.30 world units across while the token sprite renders
    /// at 0.90 — the tappable spot was a third of the token, dead centre, so taps that visibly
    /// landed on a token did nothing and you had to hunt for the middle. At the match camera
    /// that is a ~25px target on a 1080x2400 screen, about 9dp against a 48dp minimum.
    ///
    /// Sized to the sprite and capped at one board cell: going wider would spill onto the
    /// neighbouring square and start stealing taps from the token next door, which trades one
    /// selection problem for a worse one.
    /// </summary>
    private void EnsureTouchTarget()
    {
        var circle = GetComponent<CircleCollider2D>();
        if (circle == null) return;

        float scale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), 0.0001f);
        float wantedWorldRadius = touchRadiusWorld;

        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            // Half the sprite's own width, in world units, is the token as the player sees it.
            float spriteWorld = spriteRenderer.sprite.bounds.size.x * scale;
            wantedWorldRadius = Mathf.Max(wantedWorldRadius, spriteWorld * 0.5f);
        }

        float wantedLocal = wantedWorldRadius / scale;
        if (circle.radius < wantedLocal) circle.radius = wantedLocal;
    }

    // ── "you can move this one" hint ────────────────────────────────────────────────────

    [Header("Movable Hint")]
    [Tooltip("Soft breathing applied to tokens that can legally take the selected die.")]
    public float hintBreathScale = 0.055f;
    [Tooltip("Seconds for one full breath. Slow on purpose — this is a hint, not an alarm.")]
    public float hintBreathPeriod = 1.5f;
    [Tooltip("How far the token lifts toward white at the top of the breath.")]
    [Range(0f, 0.6f)] public float hintBrighten = 0.30f;

    private bool _hinting;

    /// <summary>
    /// Breathes the tokens the player could actually move right now.
    ///
    /// Chosen over dots orbiting each token: with up to four eligible tokens, four rings of
    /// travelling dots is a lot of motion competing with the move the player is about to make,
    /// and it needs its own geometry per token. A slow swell in scale and brightness reads at a
    /// glance, costs nothing, and stays calm — which is what makes it feel cinematic rather than
    /// like a tutorial arrow.
    /// </summary>
    private void Update()
    {
        bool want = ShouldHint();

        if (!want)
        {
            if (_hinting)
            {
                _hinting = false;
                transform.localScale = RestingScale;
                if (spriteRenderer != null) spriteRenderer.color = originalColor;
            }
            return;
        }

        _hinting = true;

        // Sine over unscaled time, so the hint keeps breathing while a dialog holds the game.
        float phase = Mathf.Sin(Time.unscaledTime * (Mathf.PI * 2f / Mathf.Max(0.05f, hintBreathPeriod)));
        float t = (phase + 1f) * 0.5f;

        transform.localScale = RestingScale * (1f + t * hintBreathScale);
        if (spriteRenderer != null)
            spriteRenderer.color = Color.Lerp(originalColor, Color.white, t * hintBrighten);
    }

    private bool ShouldHint()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || isFinished || IsMoving) return false;
        if (gm.currentTurnColor != tokenColor) return false;
        if (!gm.IsHumanControlled(tokenColor)) return false;
        if (gm.currentPhase != GameManager.TurnPhase.AwaitingSelection) return false;

        int value = gm.GetSelectedDiceValue();
        if (value <= 0) return false;

        // A combined roll cannot buy a token out of the yard, so those tokens are not eligible
        // and must not be advertised as if they were.
        if (isHome && gm.isDiceCombined) return false;

        return CanTokenMove(this, value);
    }

    private void OnMouseDown()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        // Someone reaching for a house that is not currently to move. Tell them why nothing
        // happened instead of ignoring the tap.
        if (gm.currentTurnColor != tokenColor)
        {
            if (UIManager.Instance != null)
            {
                Color c = LudoNX.UI.UITheme.Current.ForPlayer(gm.currentTurnColor);
                UIManager.Instance.ShowNotice(
                    gm.currentTurnColor.ToString() + " TO PLAY",
                    Color.Lerp(c, Color.white, 0.55f));
            }
            return;
        }

        if (isFinished) return;

        // Tokens are only live while the player is choosing. This rejects taps during the dice
        // spin, during another token's hop, and during post-move resolution.
        if (gm.currentPhase != GameManager.TurnPhase.AwaitingSelection) return;

        int selectedValue = gm.GetSelectedDiceValue();
        if (selectedValue <= 0)
        {
            Debug.Log("[Token] No die selected yet.");
            NudgeRejected();
            return;
        }

        if (isHome && gm.isDiceCombined)
        {
            Debug.Log("[Token] Combined dice cannot leave the yard — a single 6 is required.");
            NudgeRejected();
            return;
        }

        if (!CanTokenMove(this, selectedValue))
        {
            Debug.Log($"[Token] {tokenColor} cannot make this move (overshoot, blockade, or invalid roll).");
            NudgeRejected();
            return;
        }

        if (isHome)
        {
            // A 6 spent leaving the yard buys the yard exit and nothing else. The token lands on
            // its start tile and the die is spent — it must never also advance six squares.
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

        GameManager gm = GameManager.Instance;
        if (gm != null && gm.IsAnyTokenMoving) return;

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
        // in-flight hop, stranding the token mid-air part-way through a move.
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

    /// <summary>
    /// Feedback for a tap the rules won't allow. Replaces the console warnings these cases used
    /// to log — an ordinary mis-tap is not a developer problem, so the player gets a small shake
    /// instead of a yellow line in the console.
    /// </summary>
    private void NudgeRejected()
    {
        if (rejectShakeCoroutine != null) StopCoroutine(rejectShakeCoroutine);
        rejectShakeCoroutine = StartCoroutine(RejectShake());
    }

    private Coroutine rejectShakeCoroutine;

    private IEnumerator RejectShake()
    {
        Vector3 origin = transform.position;
        const float duration = 0.22f;
        const float amplitude = 0.07f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Three quick oscillations that decay to nothing.
            float offset = Mathf.Sin(t * Mathf.PI * 6f) * amplitude * (1f - t);
            transform.position = origin + Vector3.right * offset;
            yield return null;
        }

        transform.position = origin;
        rejectShakeCoroutine = null;
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
        GameManager gm = GameManager.Instance;

        // Claim the match-wide lock. If another token already holds it, abandon quietly rather
        // than running a second move that would consume a second die.
        if (gm != null && !gm.TryBeginTokenMove()) yield break;

        int mySequence = gm != null ? gm.TurnSequence : 0;

        isHome = false;
        currentTileIndex = 0;   // start tile, and only the start tile
        stepsTaken = 1;

        // currentTileIndex now points at the start tile, so this token counts as an occupant of it
        // from here on. IsMoving marks the transform as owned by this hop, which keeps the stack
        // layout from starting a settle coroutine that would fight the hop for the same transform.
        IsMoving = true;
        stackOffset = Vector3.zero;
        stackScale = 1f;
        if (settleCoroutine != null) { StopCoroutine(settleCoroutine); settleCoroutine = null; }

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

        IsMoving = false;

        // A start tile is a safe zone, so it is the one square an enemy token can already be
        // sitting on when this one arrives. Without this the two would stack invisibly.
        TokenStackLayout.Refresh();

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySpawn();
        CheckForCaptures();

        Debug.Log($"{tokenColor} Token moved OUT of home base onto its start tile.");

        if (gm == null) yield break;

        gm.EndTokenMove();
        if (mySequence != gm.TurnSequence) yield break;

        gm.ConsumeSelectedDice();
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

        GameManager gm = GameManager.Instance;
        if (gm != null && !gm.TryBeginTokenMove()) yield break;

        int mySequence = gm != null ? gm.TurnSequence : 0;

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
            // Leaving the stack: back to full size for the flight, and the tokens left behind
            // spread out again immediately rather than waiting for this one to land.
            IsMoving = true;
            stackOffset = Vector3.zero;
            stackScale = 1f;
            if (settleCoroutine != null) { StopCoroutine(settleCoroutine); settleCoroutine = null; }
            TokenStackLayout.Refresh();

            yield return StartCoroutine(AnimateHopSequence(waypoints, landingIsCapture));
            IsMoving = false;
        }

        currentTileIndex = newIndex;
        stepsTaken += steps;

        // Occupancy has changed on both the tile just left and the one just reached, so the whole
        // board is relaid. Doing it here — after currentTileIndex is committed but before captures
        // are resolved — means the arriving token is already counted in its new tile's stack.
        TokenStackLayout.Refresh();

        int mainLoopLength = pathLength - 6;

        if (currentTileIndex == pathLength - 1)
        {
            isFinished = true;
            // Reaching home is a token finishing, not the match being won — those deserve
            // different sounds. PlayVictory is reserved for the end of the game.
            if (AudioManager.Instance != null) AudioManager.Instance.PlayTokenHome();
            if (PointsManager.Instance != null)
            {
                PointsManager.Instance.OnTokenReachedHome(tokenColor);
            }
            if (FloatingTextFX.Instance != null)
            {
                FloatingTextFX.Instance.SpawnFloatingText(transform.position, 5);
            }
            if (gm != null)
            {
                // Third route to another roll, alongside double six and capture.
                gm.GrantHomeArrivalBonusTurn();
                gm.CheckForPlayerWin(tokenColor);
            }
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

        if (gm == null) yield break;

        gm.EndTokenMove();
        if (mySequence != gm.TurnSequence) yield break;

        gm.ConsumeSelectedDice();
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

        // Longer runs use shorter individual hops, so a six-tile move reads as a confident dash
        // rather than six deliberate jumps in a row.
        float perHop = tileCount <= 1 ? hopDurationShort
                     : tileCount <= 3 ? hopDurationMedium
                     : hopDurationLong;

        float totalDuration = perHop * tileCount;
        if (totalDuration > maxTotalMoveDuration)
        {
            perHop = maxTotalMoveDuration / tileCount;
            totalDuration = maxTotalMoveDuration;
        }
        if (landingIsCapture) totalDuration += captureHangTime;

        if (spriteRenderer != null) spriteRenderer.sortingOrder = baseSortingOrder + airborneSortingBoost;

        EnsureGroundShadow();

        int lastSoundedTile = -1;
        int lastLandedTile = -1;
        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;
            float rawT = Mathf.Clamp01(elapsed / totalDuration);

            // HORIZONTAL: one gentle ease across the whole move so it starts and stops softly.
            float easedT = Mathf.Lerp(rawT, Mathf.SmoothStep(0f, 1f, rawT), flowEasing);
            float travel = easedT * tileCount;
            int seg = Mathf.Clamp(Mathf.FloorToInt(travel), 0, tileCount - 1);
            float segT = Mathf.Clamp01(travel - seg);
            Vector3 groundPos = Vector3.Lerp(points[seg], points[seg + 1], segT);

            // VERTICAL: driven by unwarped time, so every hop rises and falls at the same rate
            // regardless of where it sits in the run.
            float hopPhase = Mathf.Repeat(rawT * totalDuration / perHop, 1f);
            if (rawT >= 1f) hopPhase = 0f;
            float arc = Mathf.Sin(hopPhase * Mathf.PI);

            transform.position = groundPos + Vector3.up * (arc * heights[seg]);
            transform.localScale = RestingScale * (1f + arc * hopScalePunch);

            UpdateGroundShadow(groundPos, arc);

            if (seg != lastSoundedTile)
            {
                lastSoundedTile = seg;
                PlayHopSound(seg, tileCount);
            }

            // Light squash each time the token touches down mid-run.
            if (seg != lastLandedTile && segT < 0.12f && seg > 0)
            {
                lastLandedTile = seg;
                ApplyInstantSquash(perHopSquashAmount * (1f - arc));
            }

            yield return null;
        }

        // Land on the seat this token holds in the destination stack, not the bare tile centre.
        transform.position = points[tileCount] + stackOffset;
        transform.localScale = RestingScale;
        UpdateGroundShadow(points[tileCount] + stackOffset, 0f);

        if (spriteRenderer != null) spriteRenderer.sortingOrder = baseSortingOrder;

        yield return StartCoroutine(LandingSquash());

        HideGroundShadow();
    }

    private void ApplyInstantSquash(float amount)
    {
        if (amount <= 0f) return;
        Vector3 rest = RestingScale;
        transform.localScale = new Vector3(
            rest.x * (1f + amount),
            rest.y * (1f - amount),
            rest.z);
    }

    #endregion

    #region Ground Shadow

    /// <summary>
    /// Creates a soft elliptical shadow under the token the first time it hops. Generated in code
    /// so it needs no imported sprite, and parented to the token's parent (not the token) so the
    /// token's own hop offset and scale punch never move it off the board plane.
    /// </summary>
    private void EnsureGroundShadow()
    {
        if (!useGroundShadow) return;

        if (shadowTransform == null)
        {
            GameObject go = new GameObject($"{name}_Shadow");
            go.transform.SetParent(transform.parent, false);

            shadowRenderer = go.AddComponent<SpriteRenderer>();
            shadowRenderer.sprite = GetSoftEllipseSprite();
            shadowRenderer.color = new Color(0f, 0f, 0f, shadowBaseAlpha);

            if (spriteRenderer != null)
            {
                shadowRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
                shadowRenderer.sortingOrder = baseSortingOrder - 1;
            }

            shadowTransform = go.transform;
        }

        shadowTransform.gameObject.SetActive(true);
    }

    private void UpdateGroundShadow(Vector3 groundPos, float arc)
    {
        if (shadowTransform == null || shadowRenderer == null) return;

        shadowTransform.position = new Vector3(
            groundPos.x + shadowOffset.x,
            groundPos.y + shadowOffset.y,
            groundPos.z + 0.01f);

        // Shrinks and fades as the token climbs.
        float scale = shadowBaseScale * Mathf.Lerp(1f, shadowApexScale, arc);
        shadowTransform.localScale = new Vector3(scale, scale * 0.55f, 1f);

        Color c = shadowRenderer.color;
        c.a = Mathf.Lerp(shadowBaseAlpha, shadowApexAlpha, arc);
        shadowRenderer.color = c;
    }

    private void HideGroundShadow()
    {
        if (shadowTransform != null) shadowTransform.gameObject.SetActive(false);
    }

    private static Sprite cachedShadowSprite;

    private static Sprite GetSoftEllipseSprite(int size = 128)
    {
        if (cachedShadowSprite != null) return cachedShadowSprite;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        float half = size * 0.5f;
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f - half) / half;
                float dy = (y + 0.5f - half) / half;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                // Solid core with a soft falloff, so the shadow reads as contact rather than fog.
                float alpha = 1f - Mathf.SmoothStep(0.35f, 1f, dist);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        cachedShadowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        cachedShadowSprite.name = "GeneratedTokenShadow";
        return cachedShadowSprite;
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
            Vector3 rest = RestingScale;
            transform.localScale = new Vector3(
                rest.x * (1f + squash),
                rest.y * (1f - squash),
                rest.z);

            yield return null;
        }

        transform.localScale = RestingScale;
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

                // The victim still gets its small marker in place — that one is about which
                // token was lost. The capturer's reward moves to the centre of the screen,
                // where it is big enough to actually read before it goes.
                if (FloatingTextFX.Instance != null)
                {
                    FloatingTextFX.Instance.SpawnFloatingText(other.transform.position, -1);
                }

                int reward = PointsManager.Instance != null ? PointsManager.Instance.capturePoints : 3;
                LudoNX.UI.CaptureBanner.Instance.Show(tokenColor, reward);

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

    /// <summary>
    /// Sends a captured token back to its yard, walking it backwards along the exact path it
    /// came down rather than teleporting.
    ///
    /// The rules state is committed immediately — the token counts as home the instant it is
    /// captured — and only the visual retreat is animated afterwards. Doing it the other way
    /// round would leave a token that the board still thinks is in play while it is flying home,
    /// and the very next move would resolve against a stale position.
    /// </summary>
    public void SendHome()
    {
        int fromIndex = currentTileIndex;

        isHome = true;
        currentTileIndex = -1;
        stepsTaken = 0;

        // Back to full size: it is alone in its yard slot now, whatever stack it was packed into.
        stackOffset = Vector3.zero;
        stackScale = 1f;
        if (settleCoroutine != null) { StopCoroutine(settleCoroutine); settleCoroutine = null; }
        transform.localScale = originalScale;

        // The tile it was captured on now holds one fewer token, so the survivors spread back out.
        TokenStackLayout.Refresh();

        if (pathScript == null) pathScript = LudoPath.FindComplete();

        bool canRetrace = isActiveAndEnabled && gameObject.activeInHierarchy
                          && fromIndex >= 0 && pathScript != null;

        if (canRetrace) StartCoroutine(RetreatAlongPath(fromIndex));
        else if (homeBaseSlot != null) transform.position = homeBaseSlot.position;
    }

    /// <summary>
    /// The walk of shame: hop back down every tile the token had climbed, in reverse, then drop
    /// into the yard slot. Faster and flatter than a forward move so it reads as a retreat
    /// rather than as a turn being taken.
    /// </summary>
    private IEnumerator RetreatAlongPath(int fromIndex)
    {
        // IsMoving keeps TokenStackLayout from snapping this token to its yard mid-flight.
        IsMoving = true;

        int boosted = baseSortingOrder + 40;
        if (spriteRenderer != null) spriteRenderer.sortingOrder = boosted;

        List<Vector3> waypoints = new List<Vector3>();
        for (int i = fromIndex - 1; i >= 0; i--)
        {
            Transform tile = pathScript.GetTileTransform(tokenColor, i);
            if (tile != null) waypoints.Add(tile.position);
        }
        if (homeBaseSlot != null) waypoints.Add(homeBaseSlot.position);

        // Budget the whole retreat rather than paying per tile: a token captured near home would
        // otherwise take ten seconds to crawl back and stall the match.
        const float totalBudget = 1.05f;
        float perHop = waypoints.Count > 0
            ? Mathf.Clamp(totalBudget / waypoints.Count, 0.035f, 0.13f)
            : 0f;

        Vector3 from = transform.position;
        for (int i = 0; i < waypoints.Count; i++)
        {
            Vector3 to = waypoints[i];
            bool lastHop = i == waypoints.Count - 1;
            float arc = lastHop ? 0.55f : 0.22f;   // a bigger lift for the drop into the yard

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(perHop, 0.0001f);
                float e = Mathf.Clamp01(t);
                Vector3 pos = Vector3.Lerp(from, to, e);
                pos.y += Mathf.Sin(e * Mathf.PI) * arc;
                transform.position = pos;
                yield return null;
            }

            transform.position = to;
            from = to;
            if (AudioManager.Instance != null && !lastHop)
                AudioManager.Instance.PlayHop(1.25f);   // higher pitch than a forward hop
        }

        if (homeBaseSlot != null) transform.position = homeBaseSlot.position;
        if (spriteRenderer != null) spriteRenderer.sortingOrder = baseSortingOrder;

        transform.localScale = originalScale;
        IsMoving = false;

        yield return StartCoroutine(LandingSquash());
        TokenStackLayout.Refresh();
    }

    /// <summary>
    /// Full reset for match start — clears the finished flag and any in-flight visual state that
    /// SendHome deliberately leaves alone, so a new match always begins from a clean board.
    /// </summary>
    public void ResetToYard()
    {
        StopAllCoroutines();

        isHome = true;
        isFinished = false;
        currentTileIndex = -1;
        stepsTaken = 0;
        isHighlighted = false;
        highlightCoroutine = null;
        rejectShakeCoroutine = null;
        IsMoving = false;

        stackOffset = Vector3.zero;
        stackScale = 1f;
        settleCoroutine = null;   // StopAllCoroutines above already killed it

        transform.localScale = originalScale == Vector3.zero ? Vector3.one : originalScale;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
            spriteRenderer.sortingOrder = baseSortingOrder;
        }

        HideGroundShadow();

        if (homeBaseSlot != null) transform.position = homeBaseSlot.position;
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
            transform.position = targetTile.position + stackOffset;
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
