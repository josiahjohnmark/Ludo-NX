using UnityEngine;

/// <summary>
/// Frames the board into the clear band between the top and bottom HUD, as large as it will go
/// without cropping or sliding under the player cards.
///
/// Two deliberate choices:
///
/// 1. It moves the CAMERA, never the board. The 52 main-loop waypoints, the four home stretches
///    and the yard slots are separate transforms positioned against the board art. Scaling the
///    sprite would leave every one of them behind; changing orthographic size magnifies the whole
///    world at once, so tile alignment is preserved exactly.
///
/// 2. It reserves space for the HUD. Fitting the board to the full screen makes it as big as
///    possible and then buries the player cards underneath it. The reserved bands below are
///    expressed in reference-resolution pixels so they track the UI layout rather than the device.
///
/// A square board in portrait is limited by WIDTH, not height, so it can never reach the ~65% of
/// screen height you'd want from a hero element. Around 55% at 9:16 is the geometric ceiling, and
/// less again on taller phones.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class BoardCameraFramer : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The board sprite. Found by name at startup when left empty.")]
    public SpriteRenderer board;
    public string boardObjectName = "Ludo board game in vibrant colours_0";

    [Header("Reserved HUD bands (reference-resolution pixels)")]
    [Tooltip("Canvas reference height these pixel values are measured against.")]
    public float referenceHeight = 1920f;
    [Tooltip("Top bar plus the upper pair of player cards.")]
    public float topReservedPx = 360f;
    [Tooltip("Lower pair of player cards, dice tray and the roll button.")]
    public float bottomReservedPx = 560f;

    [Header("Framing")]
    [Tooltip("Clear space left and right of the board, in world units.")]
    public float horizontalMargin = 0.20f;
    [Tooltip("Clear space above and below the board inside its band, in world units.")]
    public float verticalMargin = 0.25f;
    [Tooltip("Absolute bound on zoom-out. Keep generous or tall screens crop the board.")]
    public float maxOrthographicSize = 24f;
    public float minOrthographicSize = 8f;

    [Header("Dice row")]
    // The dice are world objects while the cards are screen-space UI, so a fixed world Y drifts
    // onto the cards whenever the aspect changes. Positioning and sizing them as a fraction of the
    // view keeps them locked to the band between the cards and the roll button on every device.
    [Tooltip("Distance down the screen for the dice row, 0 = top, 1 = bottom.")]
    [Range(0f, 1f)] public float diceRowScreenY = 0.855f;
    [Tooltip("Die edge length as a fraction of view height.")]
    public float diceSizeFraction = 0.036f;
    public string die1Name = "Dice_3D_1";
    public string die2Name = "Dice_3D_2";
    public string diceTrayName = "Dice_Tray";
    [Tooltip("Between the canvas plane (z=0) and the dice (z=-0.9).")]
    public float diceTrayZ = -0.5f;
    [Tooltip("Half the gap between the two dice, as a fraction of view height.")]
    public float diceSpacingFraction = 0.028f;

    private Camera cam;
    private float lastAspect;

    private void OnEnable()
    {
        cam = GetComponent<Camera>();
        Frame();
    }

    private void Update()
    {
        // Re-frame on rotation or a resized game view, not every frame's worth of work.
        if (cam != null && !Mathf.Approximately(lastAspect, cam.aspect)) Frame();
    }

    public void Frame()
    {
        if (cam == null) cam = GetComponent<Camera>();
        if (cam == null || !cam.orthographic || cam.aspect <= 0f) return;

        if (board == null && !string.IsNullOrEmpty(boardObjectName))
        {
            GameObject go = GameObject.Find(boardObjectName);
            if (go != null) board = go.GetComponent<SpriteRenderer>();
        }
        if (board == null) return;

        lastAspect = cam.aspect;

        float topFraction = Mathf.Clamp01(topReservedPx / referenceHeight);
        float bottomFraction = Mathf.Clamp01(bottomReservedPx / referenceHeight);
        float freeFraction = Mathf.Max(0.15f, 1f - topFraction - bottomFraction);

        Bounds b = board.bounds;

        // halfWidth = orthographicSize * aspect, so fitting the board across the screen needs:
        float sizeForWidth = (b.extents.x + horizontalMargin) / cam.aspect;

        // The board only gets the free band, so the full view has to be proportionally taller.
        float sizeForHeight = (b.size.y + verticalMargin * 2f) / (2f * freeFraction);

        float size = Mathf.Clamp(Mathf.Max(sizeForWidth, sizeForHeight),
                                 minOrthographicSize, maxOrthographicSize);
        cam.orthographicSize = size;

        // Centre the board in the free band rather than on the screen. With more reserved above
        // than below, the band sits low, so the camera has to rise to bring the board down.
        float viewHeight = size * 2f;
        float offset = (topFraction - bottomFraction) * 0.5f * viewHeight;

        Vector3 p = transform.position;
        p.x = b.center.x;
        p.y = b.center.y + offset;
        transform.position = p;

        PlaceDiceRow(p.y, viewHeight);
    }

    /// <summary>
    /// Anchors the dice and their tray to a screen fraction rather than a world position, so they
    /// stay in the clear band under the player cards whatever the aspect ratio.
    /// </summary>
    private void PlaceDiceRow(float cameraY, float viewHeight)
    {
        float topWorld = cameraY + viewHeight * 0.5f;
        float rowY = topWorld - diceRowScreenY * viewHeight;

        float dieSize = viewHeight * diceSizeFraction;
        float halfGap = viewHeight * diceSpacingFraction;

        PlaceDie(die1Name, -halfGap, rowY, dieSize);
        PlaceDie(die2Name, halfGap, rowY, dieSize);

        GameObject tray = GameObject.Find(diceTrayName);
        if (tray != null)
        {
            SpriteRenderer sr = tray.GetComponent<SpriteRenderer>();

            // z matters: the Screen-Space-Camera canvas sits at planeDistance from the camera,
            // which lands on z = 0 here. A tray at z = 0 is coplanar with the canvas and gets
            // painted over by the full-screen background. Sit it in front of the canvas but
            // behind the dice (which are at z = -0.9).
            tray.transform.position = new Vector3(0f, rowY, diceTrayZ);

            if (sr != null && sr.sprite != null)
            {
                // Size the well around the pair with a little breathing room.
                float wanted = (halfGap * 2f + dieSize * 2.4f);
                float native = sr.sprite.bounds.size.x;
                if (native > 0.001f) tray.transform.localScale = Vector3.one * (wanted / native);
            }
        }
    }

    private void PlaceDie(string name, float xOffset, float rowY, float size)
    {
        GameObject die = GameObject.Find(name);
        if (die == null) return;

        Vector3 pos = die.transform.position;
        die.transform.position = new Vector3(xOffset, rowY, pos.z);
        die.transform.localScale = Vector3.one * size;
    }
}
