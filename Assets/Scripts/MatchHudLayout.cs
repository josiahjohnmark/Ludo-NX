using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Owns the whole in-match HUD stack and the board's framing, laid out to the supplied reference.
///
/// Every row below is the reference's own proportion, measured off Assets/refrence.png and stored
/// as a fraction of canvas height. Driving the entire stack from one place — instead of authoring
/// rows in an editor script and separately telling the camera how much room to leave — is what
/// stops the UI and the board disagreeing, which is what buried the cards behind the board.
///
/// Two things had to be true before proportions could work at all:
///
///   • The CanvasScaler must match WIDTH (match = 0). At the default 0.5 the canvas stays
///     1080x1920 whatever the device is, so "a fraction of canvas height" is a fraction of the
///     wrong number and everything below the middle drifts down. Matching width makes the canvas
///     1080 x (1080 / aspect), so canvas height is the real device height and the fractions hold.
///   • The layout must run in the editor as well as at runtime, so it can be checked against a
///     render without entering play mode.
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
    public RectTransform audioButton;
    public RectTransform timer;

    [Header("Bottom row")]
    public RectTransform rollButton;
    public RectTransform emojiButton;
    public RectTransform chatButton;

    [Header("Board")]
    public SpriteRenderer board;

    // ── Reference proportions (fractions of canvas height) ──
    [Header("Rows — fractions of screen height")]
    [Range(0f, 1f)] public float rowIcons = 0.0815f;
    [Range(0f, 1f)] public float rowTimer = 0.1500f;
    [Range(0f, 1f)] public float rowCardsTop = 0.2415f;
    [Range(0f, 1f)] public float boardTop = 0.2870f;
    [Range(0f, 1f)] public float boardBottom = 0.7120f;
    [Range(0f, 1f)] public float rowCardsBottom = 0.7590f;
    [Range(0f, 1f)] public float rowDice = 0.8330f;
    [Range(0f, 1f)] public float rowEmoji = 0.8275f;
    [Range(0f, 1f)] public float rowRoll = 0.9150f;
    [Range(0f, 1f)] public float rowChat = 0.9225f;

    [Header("Sizes — fractions of screen width")]
    [Range(0f, 1f)] public float cardWidth = 0.392f;
    [Range(0f, 1f)] public float cardInset = 0.062f;
    [Range(0f, 1f)] public float iconSize = 0.104f;
    [Range(0f, 1f)] public float iconInset = 0.112f;
    [Range(0f, 1f)] public float rollWidth = 0.352f;
    [Range(0f, 0.3f)] public float sideMargin = 0.045f;

    private Canvas canvas;
    private Camera cam;
    private int lastW, lastH;

    private void OnEnable() { Apply(); }
    private void Start() { Apply(); }

    private void Update()
    {
        if (Screen.width == lastW && Screen.height == lastH) return;
        Apply();
    }

    /// <summary>Lays out the stack for the current screen.</summary>
    public void Apply() { ApplyFor(Screen.width, Screen.height); }

    /// <summary>
    /// Lays out for an explicit device size, so editor tooling can preview an exact phone without
    /// the editor window's own dimensions leaking into the maths.
    /// </summary>
    public void ApplyFor(int screenW, int screenH)
    {
        lastW = Screen.width;
        lastH = Screen.height;
        if (screenW <= 0 || screenH <= 0) { screenW = Screen.width; screenH = Screen.height; }

        // Resolve lazily: editor tooling calls this without Start ever having run.
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (cam == null) cam = Camera.main;
        if (board == null)
        {
            foreach (SpriteRenderer sr in FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (sr.gameObject.name.StartsWith("Ludo board")) { board = sr; break; }
            }
        }
        if (canvas == null) return;

        // Match width, so canvas height tracks the real device and the fractions mean something.
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
        {
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
        }

        float w = scaler != null ? scaler.referenceResolution.x : 1080f;
        float h = w * screenH / screenW;      // canvas height for this device, in canvas units

        // ── board framing, from the same numbers ──
        BoardCameraFramer framer = FindAnyObjectByType<BoardCameraFramer>();
        if (framer != null)
        {
            framer.referenceHeight = h;
            framer.topReservedPx = h * boardTop;
            framer.bottomReservedPx = h * (1f - boardBottom);
            framer.horizontalMargin = sideMargin;
            framer.diceRowScreenY = rowDice;
            framer.Frame();
        }

        // ── top row ──
        float icon = w * iconSize;
        PlaceCorner(exitButton, 0f, w * iconInset, h * rowIcons, icon, icon);
        PlaceCorner(audioButton, 1f, -w * iconInset, h * rowIcons, icon, icon);
        PlaceCentre(timer, h * rowTimer, w * 0.352f, h * 0.0483f);

        // ── cards ──
        float cw = w * cardWidth;
        float ch = h * 0.050f;
        float inset = w * cardInset;
        PlaceCorner(topLeft, 0f, inset, h * rowCardsTop, cw, ch);
        PlaceCorner(topRight, 1f, -inset, h * rowCardsTop, cw, ch);
        PlaceCorner(bottomLeft, 0f, inset, h * rowCardsBottom, cw, ch);
        PlaceCorner(bottomRight, 1f, -inset, h * rowCardsBottom, cw, ch);

        // ── bottom row ──
        PlaceCentre(rollButton, h * rowRoll, w * rollWidth, h * 0.0517f);
        PlaceCorner(emojiButton, 0f, w * iconInset, h * rowEmoji, icon, icon);
        PlaceCorner(chatButton, 0f, w * iconInset, h * rowChat, icon, icon);
    }

    /// <summary>Anchors to the top-left or top-right corner and positions down from the top.</summary>
    private static void PlaceCorner(RectTransform rt, float anchorX, float x, float rowFromTop, float width, float height)
    {
        if (rt == null) return;
        rt.localRotation = Quaternion.identity;   // an inherited rotation is what turned the tabs into diamonds
        rt.localScale = Vector3.one;
        rt.anchorMin = rt.anchorMax = new Vector2(anchorX, 1f);
        rt.pivot = new Vector2(anchorX, 0.5f);
        rt.anchoredPosition = new Vector2(x, -rowFromTop);
        rt.sizeDelta = new Vector2(width, height);
    }

    private static void PlaceCentre(RectTransform rt, float rowFromTop, float width, float height)
    {
        if (rt == null) return;
        rt.localRotation = Quaternion.identity;
        rt.localScale = Vector3.one;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, -rowFromTop);
        rt.sizeDelta = new Vector2(width, height);
    }
}
