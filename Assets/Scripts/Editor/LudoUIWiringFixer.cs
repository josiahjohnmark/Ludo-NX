using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// One-time Editor utility to build and wire the UIManager fields that were never created in
/// the scene (turn status banner, victory panel, stats panel, quit-confirm popup) and to attach
/// any stray duplicate Roll_Button(s) so their enabled/disabled state stays in sync. Safe to run
/// more than once — it only creates a piece if the corresponding UIManager field is still null.
/// Run from the menu, then save the scene (Ctrl+S). Everything it creates is placeholder layout
/// — reposition/restyle in the Inspector afterward.
/// </summary>
public static class LudoUIWiringFixer
{
    [MenuItem("Ludo NX/5. Build Missing Panels & Wiring")]
    public static void FixMissingUIWiring()
    {
        UIManager uiManager = Object.FindFirstObjectByType<UIManager>();
        if (uiManager == null)
        {
            EditorUtility.DisplayDialog("Ludo NX UI Fixer", "No UIManager found in the open scene. Open SampleScene and try again.", "OK");
            return;
        }

        GameObject canvasGO = GameObject.Find("Canvas");
        if (canvasGO == null)
        {
            EditorUtility.DisplayDialog("Ludo NX UI Fixer", "No GameObject named 'Canvas' found in the open scene.", "OK");
            return;
        }
        Transform canvas = canvasGO.transform;

        Undo.RecordObject(uiManager, "Fix Ludo UI Wiring");

        TMP_FontAsset headerFont = FindFont("LilitaOne-Regular");
        TMP_FontAsset bodyFont = FindFont("PlusJakartaSans-Regular");

        int created = 0;

        // 1. Turn banner — gates UpdateTurnUI() entirely when null (the root cause bug).
        if (uiManager.turnStatusText == null)
        {
            // Sits below the top bar (which occupies the first ~140px) so the two don't overlap.
            GameObject bannerPanel = CreatePanelImage("TurnBanner_Panel", canvas, new Color(0.05f, 0.06f, 0.09f, 0.85f));
            AnchorTopStretch(bannerPanel.GetComponent<RectTransform>(), yOffsetFromTop: 155f, height: 70f);

            TextMeshProUGUI statusText = CreateText("TurnStatusText", bannerPanel.transform, "RED'S TURN", headerFont, 32, TextAlignmentOptions.Center, Color.white);
            StretchFull(statusText.rectTransform);

            uiManager.turnStatusText = statusText;
            uiManager.turnBannerHeader = bannerPanel.GetComponent<Image>();
            created++;
        }

        // 2. Victory / leaderboard panel.
        if (uiManager.victoryPanel == null)
        {
            GameObject victoryPanel = CreatePanelImage("VictoryPanel", canvas, new Color(0.02f, 0.02f, 0.03f, 0.92f));
            StretchFull(victoryPanel.GetComponent<RectTransform>());

            TextMeshProUGUI leaderboardText = CreateText("LeaderboardText", victoryPanel.transform, "FINAL PLACEMENTS", bodyFont, 28, TextAlignmentOptions.Center, Color.white);
            CenterBox(leaderboardText.rectTransform, 700, 500);

            victoryPanel.SetActive(false);

            uiManager.victoryPanel = victoryPanel;
            uiManager.leaderboardText = leaderboardText;
            created++;
        }

        // 3. Post-match stats panel.
        if (uiManager.statsPanel == null)
        {
            GameObject statsPanel = CreatePanelImage("StatsPanel", canvas, new Color(0.02f, 0.02f, 0.03f, 0.92f));
            StretchFull(statsPanel.GetComponent<RectTransform>());

            TextMeshProUGUI statsText = CreateText("StatsText", statsPanel.transform, "MATCH STATS", bodyFont, 22, TextAlignmentOptions.TopLeft, Color.white);
            CenterBox(statsText.rectTransform, 800, 450);

            TextMeshProUGUI mvpText = CreateText("MvpText", statsPanel.transform, "MVP", headerFont, 26, TextAlignmentOptions.Center, new Color(1f, 0.82f, 0.2f));
            AnchorBottomCenter(mvpText.rectTransform, yOffsetFromBottom: 90f, width: 600, height: 50);

            statsPanel.SetActive(false);

            uiManager.statsPanel = statsPanel;
            uiManager.statsText = statsText;
            uiManager.mvpText = mvpText;
            created++;
        }

        // 4. Quit-confirm popup, with Yes/No buttons wired to the existing (previously unused)
        //    OnQuitConfirmed / OnQuitCancelled methods.
        if (uiManager.quitConfirmPanel == null)
        {
            GameObject quitPanel = CreatePanelImage("QuitConfirmPanel", canvas, new Color(0f, 0f, 0f, 0.6f));
            StretchFull(quitPanel.GetComponent<RectTransform>());

            GameObject box = CreatePanelImage("QuitConfirmBox", quitPanel.transform, new Color(0.1f, 0.11f, 0.14f, 1f));
            CenterBox(box.GetComponent<RectTransform>(), 420, 220);

            TextMeshProUGUI msg = CreateText("QuitConfirmText", box.transform, "Quit the match?", bodyFont, 24, TextAlignmentOptions.Center, Color.white);
            AnchorTopStretch(msg.rectTransform, yOffsetFromTop: 20f, height: 80f);

            Button yesBtn = CreateButton("QuitYesButton", box.transform, "QUIT", headerFont, new Color(0.8f, 0.2f, 0.2f));
            AnchorBottomLeftHalf(yesBtn.GetComponent<RectTransform>());
            UnityEventTools.AddPersistentListener(yesBtn.onClick, uiManager.OnQuitConfirmed);

            Button noBtn = CreateButton("QuitNoButton", box.transform, "CANCEL", headerFont, new Color(0.3f, 0.32f, 0.36f));
            AnchorBottomRightHalf(noBtn.GetComponent<RectTransform>());
            UnityEventTools.AddPersistentListener(noBtn.onClick, uiManager.OnQuitCancelled);

            quitPanel.SetActive(false);

            uiManager.quitConfirmPanel = quitPanel;
            created++;
        }

        // 5. Attach any stray duplicate Roll_Button(s) that aren't the one already wired, so
        //    SetRollButtonEnabled() keeps their interactable/color state in sync too.
        Button[] allButtons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include);
        Button[] strayRollButtons = allButtons
            .Where(b => b.gameObject.name == "Roll_Button" && b != uiManager.rollButton)
            .ToArray();

        if (strayRollButtons.Length > 0)
        {
            uiManager.extraRollButtons = strayRollButtons;
            created++;
        }

        EditorUtility.SetDirty(uiManager);
        EditorSceneManager.MarkSceneDirty(uiManager.gameObject.scene);

        EditorUtility.DisplayDialog(
            "Ludo NX UI Fixer",
            created > 0
                ? $"Created/wired {created} missing UI piece(s). SAVE THE SCENE NOW (Ctrl+S) — this only edited the open scene in memory. Everything created is placeholder layout; reposition/restyle in the Inspector as needed."
                : "Nothing to fix — every field was already assigned.",
            "OK");
    }

    private static TMP_FontAsset FindFont(string assetName)
    {
        string[] guids = AssetDatabase.FindAssets($"{assetName} t:TMP_FontAsset");
        if (guids.Length == 0) return null;
        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
    }

    private static GameObject CreatePanelImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string defaultText, TMP_FontAsset font, float size, TextAlignmentOptions alignment, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = defaultText;
        if (font != null) tmp.font = font;
        tmp.fontSize = size;
        tmp.alignment = alignment;
        tmp.color = color;
        return tmp;
    }

    private static Button CreateButton(string name, Transform parent, string label, TMP_FontAsset font, Color bgColor)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = bgColor;

        TextMeshProUGUI labelText = CreateText(name + "_Label", go.transform, label, font, 22, TextAlignmentOptions.Center, Color.white);
        StretchFull(labelText.rectTransform);

        return go.GetComponent<Button>();
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void AnchorTopStretch(RectTransform rt, float yOffsetFromTop, float height)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, height);
        rt.anchoredPosition = new Vector2(0f, -yOffsetFromTop);
    }

    private static void AnchorBottomCenter(RectTransform rt, float yOffsetFromBottom, float width, float height)
    {
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = new Vector2(0f, yOffsetFromBottom);
    }

    private static void CenterBox(RectTransform rt, float width, float height)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = Vector2.zero;
    }

    private static void AnchorBottomLeftHalf(RectTransform rt)
    {
        rt.anchorMin = new Vector2(0.05f, 0.08f);
        rt.anchorMax = new Vector2(0.48f, 0.32f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void AnchorBottomRightHalf(RectTransform rt)
    {
        rt.anchorMin = new Vector2(0.52f, 0.08f);
        rt.anchorMax = new Vector2(0.95f, 0.32f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
