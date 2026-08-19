using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lays out the whole in-match HUD against the board's actual on-screen rectangle.
///
/// The board is the only element whose size is dictated by the device — it is square and fills
/// the width, so everything else has to be arranged around wherever it lands. Measuring the board
/// each time and hanging the HUD off it is what keeps the player cards tight to the houses they
/// belong to on a 16:9 phone and a 20:9 phone alike, instead of drifting away from the board as
/// the screen gets taller.
///
/// Three things this fixes, all of which shipped broken:
///
///   • <b>CanvasScaler match.</b> At match = 1 the scaler matches HEIGHT, so on a 20:9 phone the
///     scale factor is 2400/1920 = 1.25 and a 1080-wide design is squeezed into 1080/1.25 = 864
///     canvas units. Everything authored at x = ±432 then sits exactly on the screen edge, which
///     is why the exit and sound buttons were sliced in half and the cards ran off both sides.
///     Portrait games must match WIDTH: width is the dimension the design is fixed to, and extra
///     aspect should become extra vertical room, never horizontal overflow.
///   • <b>Safe area.</b> Nothing accounted for the status bar or the gesture pill.
///   • <b>Fixed coordinates.</b> Positions were authored as constants against a 1080x1920 canvas,
///     so they could not respond to the board moving.
///
/// Sizes below are in canvas units at a 1080-wide reference, which is the design width.
/// </summary>
[ExecuteAlways]
[DefaultExecutionOrder(100)]   // after BoardCameraFramer
public class MatchHudLayout : MonoBehaviour
{
    [Header("Cards")]
    public RectTransform topLeft;
    public RectTransform topRight;
    public RectTransform bottomLeft;
    public RectTransform bottomRight;

    [Header("Top row")]
    public RectTransform exitButton;
    public RectTransform exitLabel;
    public RectTransform audioButton;
    public RectTransform audioLabel;
    public RectTransform timer;
    public RectTransform turnText;

    [Header("Bottom row")]
    public RectTransform rollButton;
    public RectTransform emojiButton;
    public RectTransform chatButton;

    [Header("Board")]
    public SpriteRenderer board;
    public string boardObjectName = "Ludo board game in vibrant colours_0";

    // ── Element sizes, canvas units at 1080 wide ──────────────────────────────────────────
    [Header("Sizes (canvas units, 1080 reference width)")]
    [Tooltip("Exit / sound / emoji / chat button edge length.")]
    public float iconButton = 92f;
    public float timerWidth = 300f;
    public float timerHeight = 84f;
    public float cardHeight = 128f;
    public float rollWidth = 400f;
    public float rollHeight = 108f;

    [Header("Spacing")]
    [Tooltip("Clear space kept inside the screen edge. Nothing is allowed to touch the edge.")]
    public float edgePadding = 24f;
    [Tooltip("Gap between a player card and the board it sits against. Deliberately tight.")]
    public float boardGap = 14f;
    [Tooltip("Gap between the top bar and the turn banner below it.")]
    public float chromeGap = 22f;
    [Tooltip("Gap between the turn banner and the cards below it.")]
    public float turnGap = 16f;
    [Tooltip("Gap between the cards, the dice and the roll row.")]
    public float stackGap = 18f;
    [Tooltip("Vertical room reserved for the dice, which are world objects.")]
    public float diceBand = 165f;

    // ── Board proportions ─────────────────────────────────────────────────────────────────
    [Header("Board proportions (fraction of board width)")]
    [Tooltip("Where the dark outer frame ends and the coloured house block begins.")]
    [Range(0f, 0.2f)] public float boardFrameInset = 0.030f;
    [Tooltip("Where a house's colour block ends. The 15x15 grid gives each house 6 cells: 6/15 = 0.40.")]
    [Range(0.2f, 0.6f)] public float houseBlockEnd = 0.400f;
    [Tooltip("Die edge length as a fraction of board width.")]
    [Range(0.03f, 0.2f)] public float dieSize = 0.130f;
    [Tooltip("Half the gap between the two dice, as a fraction of board width.")]
    [Range(0f, 0.2f)] public float diceHalfGap = 0.085f;

    private Canvas _canvas;
    private RectTransform _canvasRt;
    private Camera _cam;
    private int _lastW, _lastH;
    private Rect _lastSafe;

    private void OnEnable() { Apply(); }
    private void Start() { Apply(); }

    private void Update()
    {
        if (Screen.width == _lastW && Screen.height == _lastH && Screen.safeArea == _lastSafe) return;
        Apply();
    }

    public void Apply()
    {
        if (Screen.width <= 0 || Screen.height <= 0) return;
        _lastW = Screen.width;
        _lastH = Screen.height;
        _lastSafe = Screen.safeArea;

        if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
        if (_canvas == null) return;
        _canvasRt = (RectTransform)_canvas.transform;
        if (_cam == null) _cam = Camera.main;

        if (board == null && !string.IsNullOrEmpty(boardObjectName))
        {
            GameObject go = GameObject.Find(boardObjectName);
            if (go != null) board = go.GetComponent<SpriteRenderer>();
        }

        // ── The scaler must match width, or none of the numbers below mean anything ───────
        var scaler = _canvas.GetComponent<CanvasScaler>();
        if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
        {
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
            // The scaler applies on its own Update, so the canvas rect can still hold last
            // frame's size here. Force it now or the first layout runs against stale numbers.
            Canvas.ForceUpdateCanvases();
        }

        float canvasW = _canvasRt.rect.width;
        float canvasH = _canvasRt.rect.height;
        if (canvasW <= 1f || canvasH <= 1f) return;
        float halfW = canvasW * 0.5f;
        float halfH = canvasH * 0.5f;

        // Safe area, converted from device pixels into canvas units.
        float unitsPerPixel = canvasH / Screen.height;
        Rect sa = Screen.safeArea;
        float safeTop = Mathf.Max(0f, (Screen.height - sa.yMax) * unitsPerPixel);
        float safeBottom = Mathf.Max(0f, sa.yMin * unitsPerPixel);
        float safeLeft = Mathf.Max(0f, sa.xMin * (canvasW / Screen.width));
        float safeRight = Mathf.Max(0f, (Screen.width - sa.xMax) * (canvasW / Screen.width));

        // ── Tell the framer how much room the HUD needs, then let it size the board ───────
        // These are the only numbers the board's size depends on, so they must not be derived
        // from the board itself or the two would chase each other.
        float topReserved = safeTop + edgePadding + iconButton + chromeGap + cardHeight + boardGap;
        float bottomReserved = safeBottom + edgePadding + rollHeight + stackGap
                             + diceBand + stackGap + cardHeight + boardGap;

        var framer = FindAnyObjectByType<BoardCameraFramer>();
        if (framer != null)
        {
            framer.referenceHeight = canvasH;
            framer.topReservedPx = topReserved;
            framer.bottomReservedPx = bottomReserved;
            framer.horizontalMargin = 0f;      // the board is meant to span the full width
            framer.Frame();
        }

        // ── Measure the board where it actually landed ────────────────────────────────────
        float bLeft = -halfW, bRight = halfW, bTop = 0f, bBottom = 0f;
        if (board != null && _cam != null)
        {
            Bounds b = board.bounds;
            Vector2 lo = WorldToCanvas(b.min);
            Vector2 hi = WorldToCanvas(b.max);
            bLeft = Mathf.Min(lo.x, hi.x);
            bRight = Mathf.Max(lo.x, hi.x);
            bBottom = Mathf.Min(lo.y, hi.y);
            bTop = Mathf.Max(lo.y, hi.y);
        }
        else
        {
            // No board yet: fall back to a square centred in the free band so nothing stacks up
            // at the origin while the scene is still assembling.
            float side = Mathf.Min(canvasW, canvasH - topReserved - bottomReserved);
            bLeft = -side * 0.5f; bRight = side * 0.5f;
            float centre = (halfH - topReserved + (-halfH + bottomReserved)) * 0.5f;
            bTop = centre + side * 0.5f; bBottom = centre - side * 0.5f;
        }

        float boardW = bRight - bLeft;

        // ── Top bar ───────────────────────────────────────────────────────────────────────
        // Hung off the cards rather than pinned to the top of the screen. A 20:9 phone leaves
        // ~620 units of slack above a width-limited board; pinning the chrome to the extremes
        // spends all of it on one gap in the middle and the HUD reads as scattered. Sitting a
        // fixed distance from the cards keeps the whole thing one tight cluster around the
        // board, with the leftover space falling evenly outside it. The clamp stops it sliding
        // under the status bar on short screens.
        float topRowY = Mathf.Min(bTop + boardGap + cardHeight + turnGap + 34f + chromeGap
                                    + iconButton * 0.5f,
                                  halfH - safeTop - edgePadding - iconButton * 0.5f);
        float leftEdge = Mathf.Max(bLeft, -halfW + safeLeft + edgePadding);
        float rightEdge = Mathf.Min(bRight, halfW - safeRight - edgePadding);

        Place(exitButton, new Vector2(leftEdge + iconButton * 0.5f, topRowY), iconButton, iconButton);
        Place(audioButton, new Vector2(rightEdge - iconButton * 0.5f, topRowY), iconButton, iconButton);
        Place(timer, new Vector2(0f, topRowY), timerWidth, timerHeight);

        // The turn banner gets its own line between the bar and the cards. It used to share a
        // row with the EXIT/SOUND captions directly under the buttons, which put all three on
        // top of the cards as soon as the bar moved down to hug the board.
        float turnY = bTop + boardGap + cardHeight + turnGap + 17f;
        Place(turnText, new Vector2(0f, turnY), boardW, 34f);

        // Captions are optional; the icons carry the meaning on their own and the labels cost a
        // whole row. Kept working if a build still has them.
        float captionY = topRowY - iconButton * 0.5f - 18f;
        Place(exitLabel, new Vector2(leftEdge + iconButton * 0.5f, captionY), 180f, 26f);
        Place(audioLabel, new Vector2(rightEdge - iconButton * 0.5f, captionY), 180f, 26f);

        // ── Player cards, hung off the board ──────────────────────────────────────────────
        // Each card spans from the board's edge to where that house's colour block ends, so it
        // reads as a label for the house directly behind it and the two cards in a row are
        // separated by the width of the board's centre lanes.
        float cardLeftX = Mathf.Max(bLeft, -halfW + safeLeft + edgePadding);
        float cardRightX = Mathf.Min(bRight, halfW - safeRight - edgePadding);
        float houseEndLeft = bLeft + boardW * houseBlockEnd;
        float houseEndRight = bRight - boardW * houseBlockEnd;

        float cardW = Mathf.Max(120f, houseEndLeft - cardLeftX);
        float topCardY = bTop + boardGap + cardHeight * 0.5f;
        float bottomCardY = bBottom - boardGap - cardHeight * 0.5f;

        Place(topLeft, new Vector2(cardLeftX + cardW * 0.5f, topCardY), cardW, cardHeight);
        Place(topRight, new Vector2(cardRightX - cardW * 0.5f, topCardY), cardW, cardHeight);
        Place(bottomLeft, new Vector2(cardLeftX + cardW * 0.5f, bottomCardY), cardW, cardHeight);
        Place(bottomRight, new Vector2(cardRightX - cardW * 0.5f, bottomCardY), cardW, cardHeight);

        // ── Bottom row ────────────────────────────────────────────────────────────────────
        // Same reasoning as the top bar: measured up from the dice band under the cards, and
        // only pushed down to the safe-area floor when the screen is too short to fit it.
        float rollRowY = Mathf.Max(bBottom - boardGap - cardHeight - stackGap - diceBand
                                     - stackGap - rollHeight * 0.5f,
                                   -halfH + safeBottom + edgePadding + rollHeight * 0.5f);
        Place(rollButton, new Vector2(0f, rollRowY), rollWidth, rollHeight);
        Place(emojiButton, new Vector2(cardLeftX + iconButton * 0.5f, rollRowY), iconButton, iconButton);
        Place(chatButton, new Vector2(cardRightX - iconButton * 0.5f, rollRowY), iconButton, iconButton);

        // ── Dice: centred in whatever is left between the cards and the roll row ──────────
        if (framer != null)
        {
            float bandTop = bottomCardY - cardHeight * 0.5f;
            float bandBottom = rollRowY + rollHeight * 0.5f;
            float diceY = (bandTop + bandBottom) * 0.5f;

            framer.diceRowScreenY = Mathf.Clamp01((halfH - diceY) / canvasH);

            // Sized against the board, not the view: the board is the one fixed reference on
            // screen, so dice scaled to it stay the same size relative to the tokens they show.
            if (board != null)
            {
                float boardWorld = board.bounds.size.x;
                float viewHeight = _cam != null ? _cam.orthographicSize * 2f : 1f;
                if (viewHeight > 0.001f)
                {
                    framer.diceSizeFraction = boardWorld * dieSize / viewHeight;
                    framer.diceSpacingFraction = boardWorld * diceHalfGap / viewHeight;
                }
            }
            framer.Frame();
        }
    }

    /// <summary>World point to canvas-local coordinates, origin at the canvas centre.</summary>
    private Vector2 WorldToCanvas(Vector3 world)
    {
        Vector3 screen = _cam.WorldToScreenPoint(world);
        Camera uiCam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRt, screen, uiCam, out Vector2 local);
        return local;
    }

    /// <summary>
    /// Centre-anchors an element and places it. Centre anchoring on purpose: every position here
    /// is computed from the board, which is itself centred, so anchoring to a screen corner would
    /// only reintroduce the device-dependence this class exists to remove.
    /// </summary>
    private static void Place(RectTransform rt, Vector2 pos, float width, float height)
    {
        if (rt == null) return;
        rt.localRotation = Quaternion.identity;
        rt.localScale = Vector3.one;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = pos;
    }
}
