using System.Collections.Generic;
using System.IO;
using LudoNX.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static LudoNXUIKit;

/// <summary>
/// Builds the pre-match screen (searching → opponent found → countdown) into
/// Resources/UI/Screens/MatchFound.prefab, following the user's VS reference: a diagonal split
/// with a name above each avatar and the countdown in a pill across the middle.
///
/// The two halves are white triangles tinted at runtime with the houses each side will play, so
/// the screen states the match-up rather than decorating it. Everything sits in a Fill band, so
/// the diagonal covers the whole device and the content never stretches.
///
/// KEY MEASUREMENTS (points of the 440x956 frame)
///   your name cap centre y 150 · your avatar 118 centred at y 245
///   pill 400x150 centred · countdown number 96pt · stake line at y 350
///   opponent avatar 118 at y 700 · opponent name y 800 · cancel button at y 880
/// </summary>
public static class LudoNXMatchFoundBuilder
{
    private const string Art = "Assets/Art/UI/MatchFound";
    private const string SetupArt = "Assets/Art/UI/MatchSetup";
    private const string PrefabPath = "Assets/Resources/UI/Screens/MatchFound.prefab";

    private static readonly Dictionary<string, Vector4> Borders = new Dictionary<string, Vector4>
    {
        { "countdown_pill.png", new Vector4(60, 60, 60, 60) },
    };

    [MenuItem("Ludo NX/UI/Build Match Found Screen", priority = 5)]
    public static void Build()
    {
        ConfigureSprites(Art, Borders);
        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));

        var root = Make("MatchFound", null, typeof(CanvasGroup), typeof(MatchFoundScreen));
        Stretch(root);
        var screen = root.GetComponent<MatchFoundScreen>();
        screen.SetId(ScreenId.MatchFound);

        var bg = Make("Background", root, typeof(Image));
        Stretch(bg);
        bg.GetComponent<Image>().color = Slate;
        bg.GetComponent<Image>().raycastTarget = false;

        // The split covers the device, not the design frame: the diagonal must reach every corner
        // on a tall phone as well as an iPad, so these two stretch while the content scales.
        screen.splitTop = FullBleed(root, "SplitTop", $"{Art}/split_top.png");
        screen.splitBottom = FullBleed(root, "SplitBottom", $"{Art}/split_bottom.png");

        var band = Band(root, "ContentBand", DesignBand.Edge.Fill, 0f);
        var content = Make("Content", band);
        Stretch(content);
        var bandComp = band.GetComponent<DesignBand>();
        bandComp.safeAreaBaseline = SafeTop;
        bandComp.safeAreaContent = content;

        BuildYou(content, screen);
        BuildOpponent(content, screen);
        BuildCentre(content, screen);
        BuildCancel(content, screen);

        screen.pawnSprites = new[]
        {
            Sprite($"{SetupArt}/pawn_red.png"),
            Sprite($"{SetupArt}/pawn_green.png"),
            Sprite($"{SetupArt}/pawn_yellow.png"),
            Sprite($"{SetupArt}/pawn_blue.png"),
        };
        screen.defaultPortrait = Sprite($"{HomeArt}/TopBar/profile_pics.png");
        screen.avatars = LoadAvatars();

        PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath);
        Object.DestroyImmediate(root.gameObject);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Ludo NX] Match Found screen built -> {PrefabPath}.");
    }

    private static Image FullBleed(RectTransform parent, string name, string spritePath)
    {
        var img = Picture(parent, name, spritePath);
        var rt = (RectTransform)img.transform;
        Stretch(rt);
        img.preserveAspect = false;
        return img;
    }

    // ======================================================================================
    // sides
    // ======================================================================================

    private static void BuildYou(RectTransform parent, MatchFoundScreen screen)
    {
        var name = Text(parent, "YourName", "JOSIAH", 40f, Cream, TextAlignmentOptions.Center);
        Top(name.rectTransform, H.Centre, 220f, 150f, 400f, 54f);
        screen.yourName = name;

        var avatar = Avatar(parent, "YourAvatar", 245f, out Image portrait);
        screen.yourPortrait = portrait;

        var rating = Text(parent, "YourRating", "0 NX", 15f, new Color32(255, 242, 220, 220),
            TextAlignmentOptions.Center);
        Top(rating.rectTransform, H.Centre, 220f, 320f, 240f, 22f);
        screen.yourRating = rating;

        screen.yourPawns = PawnPair(parent, "YourPawns", 352f, 0, 2);
    }

    private static void BuildOpponent(RectTransform parent, MatchFoundScreen screen)
    {
        var group = Make("OpponentGroup", parent, typeof(CanvasGroup));
        Stretch(group);
        screen.opponentGroup = group.GetComponent<CanvasGroup>();

        screen.opponentPawns = PawnPair(group, "OpponentPawns", 604f, 1, 3);

        var rating = Text(group, "OpponentRating", "1,200 NX", 15f, new Color32(255, 242, 220, 220),
            TextAlignmentOptions.Center);
        Top(rating.rectTransform, H.Centre, 220f, 640f, 240f, 22f);
        screen.opponentRating = rating;

        var avatar = Avatar(group, "OpponentAvatar", 718f, out Image portrait);
        screen.opponentPortrait = portrait;

        var style = Text(group, "OpponentStyle", "Hunter", 13f, new Color32(255, 242, 220, 190),
            TextAlignmentOptions.Center);
        Top(style.rectTransform, H.Centre, 220f, 790f, 240f, 20f);
        screen.opponentStyle = style;

        var name = Text(group, "OpponentName", "AMARA O.", 40f, Cream, TextAlignmentOptions.Center);
        Top(name.rectTransform, H.Centre, 220f, 830f, 400f, 54f);
        screen.opponentName = name;
    }

    /// <summary>Portrait in the game's ring, matching the avatar treatment on Home and Friends.</summary>
    private static RectTransform Avatar(RectTransform parent, string name, float cy, out Image portrait)
    {
        var holder = Make(name, parent);
        Top(holder, H.Centre, 220f, cy, 124f, 124f);

        portrait = Picture(holder, "Portrait", $"{HomeArt}/TopBar/profile_pics.png");
        Centre((RectTransform)portrait.transform, 0f, -2f, 100f, 100f);

        var ring = Picture(holder, "Ring", $"{HomeArt}/TopBar/profile_frame.png");
        Centre((RectTransform)ring.transform, 0f, 0f, 124f, 124f);

        var crest = Picture(holder, "Crest", $"{HomeArt}/TopBar/ludo_nx_badge_for_profile_frame.png");
        Centre((RectTransform)crest.transform, 0f, -50f, 30f, 34f);

        return holder;
    }

    private static Image[] PawnPair(RectTransform parent, string name, float cy, int first, int second)
    {
        var holder = Make(name, parent);
        Top(holder, H.Centre, 220f, cy, 140f, 40f);

        var a = Picture(holder, "PawnA", $"{SetupArt}/pawn_red.png");
        Centre((RectTransform)a.transform, -24f, 0f, 34f, 34f);

        var b = Picture(holder, "PawnB", $"{SetupArt}/pawn_yellow.png");
        Centre((RectTransform)b.transform, 24f, 0f, 34f, 34f);

        return new[] { a, b };
    }

    // ======================================================================================
    // centre: searching and countdown
    // ======================================================================================

    private static void BuildCentre(RectTransform parent, MatchFoundScreen screen)
    {
        var pill = Make("Pill", parent, typeof(Image));
        Top(pill, H.Centre, 220f, 478f, 400f, 150f);
        Sliced(pill.GetComponent<Image>(), $"{Art}/countdown_pill.png", 2f);
        pill.GetComponent<Image>().raycastTarget = false;

        // Searching state
        var searching = Make("SearchingGroup", pill, typeof(CanvasGroup));
        Stretch(searching);
        screen.searchingGroup = searching.GetComponent<CanvasGroup>();

        var spinner = Picture(searching, "Spinner", $"{FriendsArt}/spinner.png");
        Centre((RectTransform)spinner.transform, 0f, 26f, 34f, 34f);
        screen.spinner = (RectTransform)spinner.transform;

        var label = Text(searching, "SearchingLabel", "Searching for an opponent", 17f, Brown,
            TextAlignmentOptions.Center);
        Centre(label.rectTransform, 0f, -18f, 380f, 28f);
        screen.searchingLabel = label;

        // Countdown state
        var countdown = Make("CountdownGroup", pill, typeof(CanvasGroup));
        Stretch(countdown);
        screen.countdownGroup = countdown.GetComponent<CanvasGroup>();

        var number = Text(countdown, "Number", "3", 96f, new Color32(20, 16, 12, 255),
            TextAlignmentOptions.Center);
        Centre(number.rectTransform, 0f, 0f, 380f, 130f);
        screen.countdownNumber = number;

        var stake = Text(parent, "StakeLabel", "x1   ·   Win +20 NX", 15f, Cream, TextAlignmentOptions.Center);
        Top(stake.rectTransform, H.Centre, 220f, 572f, 400f, 24f);
        screen.stakeLabel = stake;
    }

    private static void BuildCancel(RectTransform parent, MatchFoundScreen screen)
    {
        var cancel = Make("CancelButton", parent, typeof(Image), typeof(Button));
        Top(cancel, H.Centre, 220f, 892f, 180f, 48f);
        Sliced(cancel.GetComponent<Image>(), $"{FriendsArt}/btn_blank_red.png", 2f);
        screen.cancelButton = cancel.GetComponent<Button>();

        var label = Text(cancel, "Label", "CANCEL", 18f, Cream, TextAlignmentOptions.Center);
        Stretch(label.rectTransform);
        label.rectTransform.offsetMin = new Vector2(0f, 3f);
        screen.cancelLabel = label;
    }

    private static Sprite[] LoadAvatars()
    {
        var list = new List<Sprite>();
        for (int i = 0; i < 64; i++)
        {
            string path = $"{FriendsArt}/Avatars/avatar_gen_{i:00}.png";
            if (!File.Exists(path)) break;
            list.Add(Sprite(path));
        }
        return list.ToArray();
    }
}
