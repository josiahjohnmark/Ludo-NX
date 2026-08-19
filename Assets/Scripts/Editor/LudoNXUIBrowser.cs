using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// A way into the UI from the editor.
///
/// Two things make this UI hard to find by hand, and neither is obvious from the Scene view:
///
///   • The screens are not in the scene. They are prefabs under Resources/UI, and ScreenManager
///     instantiates whichever one it needs at runtime. Looking at the scene shows you the board
///     and nothing else, because at edit time there genuinely is nothing else.
///   • The match HUD IS in the scene, but a Screen-Space-Overlay canvas is drawn at one world
///     unit per canvas unit. The canvas is 1080x2400 units across while the board is about 13.8,
///     so framing the board puts the HUD roughly 78x off-screen.
///
/// This window opens any screen in Prefab Mode and frames the HUD in the Scene view.
/// </summary>
public class LudoNXUIBrowser : EditorWindow
{
    private const string ScreenDir = "Assets/Resources/UI/Screens";
    private const string ModalDir = "Assets/Resources/UI/Modals";

    private Vector2 _scroll;

    [MenuItem("Ludo NX/UI Browser", priority = 0)]
    public static void Open()
    {
        var w = GetWindow<LudoNXUIBrowser>("Ludo NX UI");
        w.minSize = new Vector2(260f, 320f);
        w.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Match HUD", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "The HUD lives in the scene. It is laid out by MatchHudLayout on the same object — " +
            "select it to tune sizes and spacing, and the Scene view updates as you type.",
            MessageType.None);

        if (GUILayout.Button("Select + frame HUD in Scene view", GUILayout.Height(26f)))
            FrameHud();

        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField("Screens", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "These are prefabs, not scene objects. Opening one enters Prefab Mode, where you can " +
            "see and edit it on its own.\n\n" +
            "Careful: the 'Build ALL Screens' generators rewrite these prefabs from code, so hand " +
            "edits are lost on the next rebuild. For changes you want to keep, edit the authoring " +
            "script instead.",
            MessageType.Warning);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        DrawGroup(ScreenDir);
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Modals", EditorStyles.boldLabel);
        DrawGroup(ModalDir);
        EditorGUILayout.EndScrollView();
    }

    private static void DrawGroup(string dir)
    {
        foreach (string path in PrefabsIn(dir))
        {
            if (!GUILayout.Button(Path.GetFileNameWithoutExtension(path), GUILayout.Height(22f))) continue;

            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset != null) AssetDatabase.OpenAsset(asset);
        }
    }

    private static List<string> PrefabsIn(string dir)
    {
        var found = new List<string>();
        if (!Directory.Exists(dir)) return found;
        foreach (string f in Directory.GetFiles(dir, "*.prefab"))
            found.Add(f.Replace('\\', '/'));
        found.Sort();
        return found;
    }

    /// <summary>
    /// Selects the HUD canvas and frames it, which is the only practical way to look at an
    /// overlay canvas that is three orders of magnitude larger than the board beside it.
    /// </summary>
    private static void FrameHud()
    {
        var layout = Object.FindFirstObjectByType<MatchHudLayout>(FindObjectsInactive.Include);
        GameObject target = layout != null ? layout.gameObject : null;

        if (target == null)
        {
            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,
                                                              FindObjectsSortMode.None))
            {
                if (!c.gameObject.scene.IsValid()) continue;
                if (c.sortingOrder == 150) { target = c.gameObject; break; }
            }
        }

        if (target == null)
        {
            EditorUtility.DisplayDialog("Ludo NX",
                "No match HUD canvas in this scene. Run Ludo NX ▸ UI ▸ 10. Rebuild Match HUD first.",
                "OK");
            return;
        }

        Selection.activeGameObject = target;
        EditorGUIUtility.PingObject(target);

        var view = SceneView.lastActiveSceneView;
        if (view != null)
        {
            view.in2DMode = true;          // an overlay canvas is flat; perspective only fights you
            view.FrameSelected();
            view.Repaint();
        }
    }
}
