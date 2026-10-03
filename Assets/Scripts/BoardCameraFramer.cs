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

    [Header("Centering & Alignment")]
    [Tooltip("Keep the board directly in the center of the screen.")]
    public bool centerInScreen = true;

    [Header("World Background")]
    [Tooltip("Keep the dark embossed wood wallpaper behind the board in world space so UI canvas never blocks the board.")]
    public bool manageWorldBackground = true;
    public string backgroundObjectName = "MatchBackground_World";
    public string backgroundSpritePath = "Assets/Art/UI/Backgrounds/bg_match_emboss.jpg";

    [Header("Reserved HUD bands (reference-resolution pixels)")]
    [Tooltip("Canvas reference height these pixel values are measured against.")]
    public float referenceHeight = 1920f;
    [Tooltip("Top bar plus the upper pair of player cards.")]
    public float topReservedPx = 360f;
    [Tooltip("Lower pair of player cards, dice tray and the roll button.")]
    public float bottomReservedPx = 560f;

    [Header("Framing")]
    [Tooltip("Clear space left and right of the board, in world units. Set to -0.40f to span edge-to-edge while preventing card overlap.")]
    public float horizontalMargin = -0.40f;
    [Tooltip("Clear space above and below the board inside its band, in world units.")]
    public float verticalMargin = 0.25f;
    [Tooltip("Absolute bound on zoom-out. Keep generous or tall screens crop the board.")]
    public float maxOrthographicSize = 24f;
    public float minOrthographicSize = 8f;

    [Header("Dice row")]
    // The dice are world objects while the cards are screen-space UI.
    // 0.753f aligns with cy=720 on 956pt reference canvas (DiceTrayWell).
    [Tooltip("Distance down the screen for the dice row, 0 = top, 1 = bottom.")]
    [Range(0f, 1f)] public float diceRowScreenY = 0.753f;
    [Tooltip("Die edge length as a fraction of view height.")]
    public float diceSizeFraction = 0.088f;
    public string die1Name = "Dice_3D_1";
    public string die2Name = "Dice_3D_2";
    public string diceTrayName = "Dice_Tray";
    [Tooltip("Between the canvas plane (z=0) and the dice (z=-0.9).")]
    public float diceTrayZ = -0.5f;
    [Tooltip("Half the gap between the two dice, as a fraction of view height.")]
    public float diceSpacingFraction = 0.038f;

    private Camera cam;
    private float lastAspect;
    private SpriteRenderer _bgRenderer;
    private GameObject _lastTrayWell;

    private void OnEnable()
    {
        cam = GetComponent<Camera>();
        Frame();
    }

    private void Update()
    {
        // Re-frame on rotation or a resized game view
        bool aspectChanged = cam != null && !Mathf.Approximately(lastAspect, cam.aspect);
        GameObject tray = GameObject.Find("DiceTrayWell");
        bool trayChanged = tray != _lastTrayWell;

        if (aspectChanged || trayChanged)
        {
            _lastTrayWell = tray;
            Frame();
        }
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
        Bounds b = board.bounds;

        float sizeForWidth = (b.extents.x + horizontalMargin) / cam.aspect;
        float size;

        if (centerInScreen)
        {
            // Size camera so board spans edge-to-edge horizontally touching both left and right edges
            size = Mathf.Clamp(sizeForWidth, minOrthographicSize, maxOrthographicSize);
            cam.orthographicSize = size;

            Vector3 p = transform.position;
            p.x = b.center.x;
            p.y = b.center.y; // Centered directly on board!
            transform.position = p;
        }
        else
        {
            float topFraction = Mathf.Clamp01(topReservedPx / referenceHeight);
            float bottomFraction = Mathf.Clamp01(bottomReservedPx / referenceHeight);
            float freeFraction = Mathf.Max(0.15f, 1f - topFraction - bottomFraction);

            float sizeForHeight = (b.size.y + verticalMargin * 2f) / (2f * freeFraction);
            size = Mathf.Clamp(Mathf.Max(sizeForWidth, sizeForHeight), minOrthographicSize, maxOrthographicSize);
            cam.orthographicSize = size;

            float viewHeight = size * 2f;
            float offset = (topFraction - bottomFraction) * 0.5f * viewHeight;

            Vector3 p = transform.position;
            p.x = b.center.x;
            p.y = b.center.y + offset;
            transform.position = p;
        }

        float totalViewHeight = size * 2f;
        PlaceDiceRow(transform.position.y, totalViewHeight);
        UpdateWorldBackground(b);
    }

    /// <summary>
    /// Anchors the dice and their tray to the HUD's DiceTrayWell or a screen fraction.
    /// </summary>
    private void PlaceDiceRow(float cameraY, float viewHeight)
    {
        float rowY;
        float dieSize = viewHeight * diceSizeFraction;
        float halfGap = viewHeight * diceSpacingFraction;

        GameObject sockL = GameObject.Find("SocketLeft");
        GameObject sockR = GameObject.Find("SocketRight");
        if (sockL != null && sockR != null && cam != null)
        {
            var rtL = sockL.GetComponent<RectTransform>();
            var rtR = sockR.GetComponent<RectTransform>();
            Canvas c = sockL.GetComponentInParent<Canvas>();

            Vector3 worldPosL, worldPosR;
            float socketWorldW;

            if (c != null && c.renderMode == RenderMode.ScreenSpaceCamera)
            {
                // In ScreenSpaceCamera, rt.GetWorldCorners returns world positions directly
                Vector3[] corners = new Vector3[4];
                rtL.GetWorldCorners(corners);
                socketWorldW = Mathf.Abs(corners[2].x - corners[0].x);
                worldPosL = sockL.transform.position;
                worldPosR = sockR.transform.position;
            }
            else
            {
                // In ScreenSpaceOverlay, rt.transform.position and corners are in screen pixel space
                Vector3[] corners = new Vector3[4];
                rtL.GetWorldCorners(corners);
                Vector3 p0 = cam.ScreenToWorldPoint(new Vector3(corners[0].x, corners[0].y, -cam.transform.position.z));
                Vector3 p2 = cam.ScreenToWorldPoint(new Vector3(corners[2].x, corners[2].y, -cam.transform.position.z));
                socketWorldW = Mathf.Abs(p2.x - p0.x);

                worldPosL = cam.ScreenToWorldPoint(new Vector3(sockL.transform.position.x, sockL.transform.position.y, -cam.transform.position.z + diceTrayZ));
                worldPosR = cam.ScreenToWorldPoint(new Vector3(sockR.transform.position.x, sockR.transform.position.y, -cam.transform.position.z + diceTrayZ));
            }

            float fittedDieSize = socketWorldW > 0.1f ? socketWorldW * 0.74f : 1.0f;
            PlaceDie(die1Name, worldPosL.x, worldPosL.y, fittedDieSize);
            PlaceDie(die2Name, worldPosR.x, worldPosR.y, fittedDieSize);
            return;
        }

        GameObject trayWell = GameObject.Find("DiceTrayWell");
        if (trayWell != null && cam != null)
        {
            var rt = trayWell.GetComponent<RectTransform>();
            Canvas c = trayWell.GetComponentInParent<Canvas>();
            if (c != null && c.renderMode == RenderMode.ScreenSpaceCamera)
            {
                rowY = rt.position.y;
            }
            else
            {
                Vector3[] corners = new Vector3[4];
                rt.GetWorldCorners(corners);
                Vector3 screenCenter = (corners[0] + corners[2]) * 0.5f;
                Vector3 worldPt = cam.ScreenToWorldPoint(new Vector3(screenCenter.x, screenCenter.y, -cam.transform.position.z + diceTrayZ));
                rowY = worldPt.y;
            }
        }
        else
        {
            float topWorld = cameraY + viewHeight * 0.5f;
            rowY = topWorld - diceRowScreenY * viewHeight;
        }

        PlaceDie(die1Name, -halfGap, rowY, dieSize);
        PlaceDie(die2Name, halfGap, rowY, dieSize);

        GameObject tray = GameObject.Find(diceTrayName);
        if (tray != null)
        {
            SpriteRenderer sr = tray.GetComponent<SpriteRenderer>();
            tray.transform.position = new Vector3(0f, rowY, diceTrayZ);

            if (sr != null && sr.sprite != null)
            {
                float wanted = (halfGap * 2f + dieSize * 2.4f);
                float native = sr.sprite.bounds.size.x;
                if (native > 0.001f) tray.transform.localScale = Vector3.one * (wanted / native);
            }
        }
    }

    private void PlaceDie(string name, float xOffset, float rowY, float size)
    {
        GameObject die = GameObject.Find(name);
        if (die == null)
        {
            var all = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var go in all)
            {
                if (go.name == name && go.scene.isLoaded)
                {
                    die = go;
                    break;
                }
            }
        }
        if (die == null) return;

        die.SetActive(true);
        die.transform.position = new Vector3(xOffset, rowY, diceTrayZ);
        die.transform.localScale = Vector3.one * size;
        die.transform.rotation = Quaternion.identity;
    }

    private void UpdateWorldBackground(Bounds boardBounds)
    {
        if (!manageWorldBackground) return;

        if (_bgRenderer == null)
        {
            GameObject bgGo = GameObject.Find(backgroundObjectName);
            if (bgGo == null)
            {
                bgGo = new GameObject(backgroundObjectName);
            }
            _bgRenderer = bgGo.GetComponent<SpriteRenderer>();
            if (_bgRenderer == null) _bgRenderer = bgGo.AddComponent<SpriteRenderer>();
            _bgRenderer.sortingLayerName = "Default";
            _bgRenderer.sortingOrder = -100;

            if (_bgRenderer.sprite == null && !string.IsNullOrEmpty(backgroundSpritePath))
            {
#if UNITY_EDITOR
                _bgRenderer.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(backgroundSpritePath);
#endif
            }
        }

        if (_bgRenderer != null && _bgRenderer.sprite != null && cam != null)
        {
            _bgRenderer.transform.position = new Vector3(boardBounds.center.x, boardBounds.center.y, 5f);
            float camH = cam.orthographicSize * 2f;
            float camW = camH * cam.aspect;
            float sw = _bgRenderer.sprite.bounds.size.x;
            float sh = _bgRenderer.sprite.bounds.size.y;
            if (sw > 0.001f && sh > 0.001f)
            {
                float scale = Mathf.Max(camW / sw, camH / sh) * 1.05f;
                _bgRenderer.transform.localScale = new Vector3(scale, scale, 1f);
            }
        }
    }
}
