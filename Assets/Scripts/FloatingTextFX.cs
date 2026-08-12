using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Object-pooled floating text effect for point changes.
/// Spawns "+3 PTS" / "+5 PTS" / "-1 PT" text that rises and fades.
/// Gold for positive, red for negative.
///
/// Attach to the Canvas. If no prefab is assigned it builds its own text objects, so the effect
/// works with zero Inspector setup.
/// </summary>
public class FloatingTextFX : MonoBehaviour
{
    [Header("Prefab (optional — one is generated if left empty)")]
    public GameObject floatingTextPrefab;

    [Header("Font (optional — falls back to the TMP default)")]
    public TMP_FontAsset fontAsset;

    [Header("Animation Settings")]
    public float riseDuration = 0.8f;
    public float riseDistance = 60f;  // Canvas units to rise
    public float fadeDelay = 0.3f;    // Start fading after this
    public float fontSize = 40f;

    [Header("Colors")]
    public Color positiveColor = new Color(1f, 0.85f, 0.2f);  // Gold
    public Color negativeColor = new Color(1f, 0.3f, 0.3f);    // Red

    private static FloatingTextFX instance;

    public static FloatingTextFX Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<FloatingTextFX>();
            }
            return instance;
        }
    }

    private readonly Queue<GameObject> pool = new Queue<GameObject>();
    private Canvas parentCanvas;

    private void Awake()
    {
        instance = this;
        parentCanvas = GetComponentInParent<Canvas>();
    }

    /// <summary>
    /// Spawn a floating points text at a world position (e.g. where a capture happened).
    /// </summary>
    public void SpawnFloatingText(Vector3 worldPosition, int delta)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector2 screenPoint = cam.WorldToScreenPoint(worldPosition);
        SpawnAtScreenPoint(screenPoint, delta);
    }

    /// <summary>
    /// Spawn a floating points text over a UI element (for player card events).
    /// </summary>
    public void SpawnFloatingTextAtUI(RectTransform uiElement, int delta)
    {
        if (uiElement == null) return;

        Camera uiCam = GetUICamera();
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCam, uiElement.position);
        SpawnAtScreenPoint(screenPoint, delta);
    }

    private void SpawnAtScreenPoint(Vector2 screenPoint, int delta)
    {
        RectTransform canvasRect = transform as RectTransform;
        if (canvasRect == null) return;

        // A Screen Space - Camera canvas needs the screen point converted into canvas-local space;
        // assigning the raw pixel coordinate to .position would place the text far off-screen.
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPoint, GetUICamera(), out Vector2 localPoint))
        {
            return;
        }

        GameObject textObj = Rent();
        RectTransform rect = textObj.GetComponent<RectTransform>();
        if (rect != null) rect.anchoredPosition = localPoint;

        TextMeshProUGUI tmp = textObj.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            string sign = delta >= 0 ? "+" : "";
            string label = Mathf.Abs(delta) == 1 ? "PT" : "PTS";
            tmp.text = $"{sign}{delta} {label}";
            tmp.color = delta >= 0 ? positiveColor : negativeColor;
        }

        StartCoroutine(AnimateFloatingText(textObj));
    }

    private Camera GetUICamera()
    {
        if (parentCanvas == null) parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null) return null;
        return parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera;
    }

    private GameObject Rent()
    {
        while (pool.Count > 0)
        {
            GameObject pooled = pool.Dequeue();
            if (pooled != null)
            {
                pooled.SetActive(true);
                return pooled;
            }
        }

        return Create();
    }

    private void Release(GameObject obj)
    {
        if (obj == null) return;
        obj.SetActive(false);
        pool.Enqueue(obj);
    }

    private GameObject Create()
    {
        if (floatingTextPrefab != null)
        {
            return Instantiate(floatingTextPrefab, transform);
        }

        GameObject go = new GameObject("FloatingText", typeof(RectTransform));
        go.transform.SetParent(transform, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(300f, 80f);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) tmp.font = fontAsset;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        go.AddComponent<CanvasGroup>();
        return go;
    }

    private IEnumerator AnimateFloatingText(GameObject textObj)
    {
        RectTransform rect = textObj.GetComponent<RectTransform>();
        CanvasGroup cg = textObj.GetComponent<CanvasGroup>();
        if (cg == null) cg = textObj.AddComponent<CanvasGroup>();

        Vector2 startPos = rect != null ? rect.anchoredPosition : Vector2.zero;
        float fadeStart = riseDuration > 0f ? fadeDelay / riseDuration : 0f;

        float elapsed = 0f;
        cg.alpha = 1f;

        while (elapsed < riseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / riseDuration);

            if (rect != null)
            {
                rect.anchoredPosition = startPos + Vector2.up * (riseDistance * t);
            }

            cg.alpha = t > fadeStart
                ? 1f - Mathf.Clamp01((t - fadeStart) / Mathf.Max(0.0001f, 1f - fadeStart))
                : 1f;

            // Scale pop: starts at 1.2x, settles to 1x
            textObj.transform.localScale = Vector3.one * Mathf.Lerp(1.2f, 1f, t);

            yield return null;
        }

        Release(textObj);
    }
}
