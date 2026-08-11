using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Object-pooled floating text effect for point changes.
/// Spawns "+3 PTS" / "+5 PTS" / "-1 PT" text that rises and fades.
/// Gold for positive, red for negative.
/// </summary>
public class FloatingTextFX : MonoBehaviour
{
    [Header("Prefab References")]
    public GameObject floatingTextPrefab;  // A UI TextMeshPro prefab

    [Header("Animation Settings")]
    public float riseDuration = 0.8f;
    public float riseDistance = 60f;  // Pixels to rise
    public float fadeDelay = 0.3f;    // Start fading after this

    [Header("Colors")]
    public Color positiveColor = new Color(1f, 0.85f, 0.2f);  // Gold
    public Color negativeColor = new Color(1f, 0.3f, 0.3f);    // Red

    // Simple object pool
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

    private void Awake()
    {
        instance = this;
    }

    /// <summary>
    /// Spawn a floating points text at a world position.
    /// </summary>
    /// <param name="worldPosition">Where on the board the event happened.</param>
    /// <param name="delta">Point change (+3, +5, -1, etc.)</param>
    public void SpawnFloatingText(Vector3 worldPosition, int delta)
    {
        if (floatingTextPrefab == null) return;

        // Convert world position to screen position
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 screenPos = cam.WorldToScreenPoint(worldPosition);

        // Instantiate the floating text
        GameObject textObj = Instantiate(floatingTextPrefab, transform);
        RectTransform rect = textObj.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.position = screenPos;
        }

        // Set text and color
        TextMeshProUGUI tmp = textObj.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            string sign = delta >= 0 ? "+" : "";
            string label = Mathf.Abs(delta) == 1 ? "PT" : "PTS";
            tmp.text = $"{sign}{delta} {label}";
            tmp.color = delta >= 0 ? positiveColor : negativeColor;
        }

        // Animate and destroy
        StartCoroutine(AnimateFloatingText(textObj));
    }

    /// <summary>
    /// Spawn floating text at a UI element's position (for player card events).
    /// </summary>
    public void SpawnFloatingTextAtUI(RectTransform uiElement, int delta)
    {
        if (floatingTextPrefab == null || uiElement == null) return;

        GameObject textObj = Instantiate(floatingTextPrefab, transform);
        RectTransform rect = textObj.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.position = uiElement.position;
        }

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

    private IEnumerator AnimateFloatingText(GameObject textObj)
    {
        RectTransform rect = textObj.GetComponent<RectTransform>();
        CanvasGroup cg = textObj.GetComponent<CanvasGroup>();

        if (cg == null)
        {
            cg = textObj.AddComponent<CanvasGroup>();
        }

        Vector3 startPos = rect != null ? rect.position : textObj.transform.position;
        float elapsed = 0f;
        cg.alpha = 1f;

        while (elapsed < riseDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / riseDuration;

            // Rise upward
            if (rect != null)
            {
                rect.position = startPos + Vector3.up * (riseDistance * t);
            }

            // Fade out in the second half
            if (t > fadeDelay / riseDuration)
            {
                float fadeT = (t - fadeDelay / riseDuration) / (1f - fadeDelay / riseDuration);
                cg.alpha = 1f - fadeT;
            }

            // Scale pop: starts at 1.2x, settles to 1x
            float scale = Mathf.Lerp(1.2f, 1f, t);
            textObj.transform.localScale = Vector3.one * scale;

            yield return null;
        }

        Destroy(textObj);
    }
}
