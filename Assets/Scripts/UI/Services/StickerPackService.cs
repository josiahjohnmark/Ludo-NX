using System.Collections.Generic;
using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// Emoji sticker packs: what exists, what the player owns, and buying with energy.
    ///
    /// Owner decision, 27 Sep 2026: emoji packs are priced in in-game energy, not real money
    /// (energy itself is what is bought through RevenueCat — see Shop/EnergyCatalog).
    ///
    /// Stickers are the animated flipbooks already in the project: the ten match emotes
    /// (Art/UI/Emotes, 104 px cells) and the eight emoji strips (Art/UI/MatchSetup/Emoji,
    /// 128 px cells). The CLASSIC pack is the free starter — it is the match tray's default
    /// eight, so every player owns it from the first launch.
    ///
    /// NOTE (go-live): local and trusting the phone, like EnergyService. Moves to the server
    /// `buyWithCoins`-style function with T-A3 (GO_LIVE_PLAN §5).
    /// </summary>
    public static class StickerPackService
    {
        public class Sticker
        {
            public string id;
            public string name;
            public string path;       // asset path, used by the editor builder to bake textures
            public int frames;
            public int content;
            public float fps;
        }

        public class Pack
        {
            public string id;
            public string name;
            public string rarity;     // tokens.json color.rarity key
            public Color foil;        // tokens.json color.rarity.<rarity>.hex
            public int energyCost;    // 0 = free starter
            public string[] stickers;
        }

        private const string Emotes = "Assets/Art/UI/Emotes/";
        private const string Emoji = "Assets/Art/UI/MatchSetup/Emoji/";

        private static Sticker Emote(string n, float fps) => new Sticker
            { id = "emote_" + n, name = n.ToUpperInvariant(), path = Emotes + "emote_" + n + ".png", frames = 8, content = 96, fps = fps };

        // Own display names: four emoji share a subject with an emote (fire, cool, party,
        // thinking), and two stickers both called FIRE read as a duplicate in the Album.
        private static Sticker Emo(string n, string display, int frames = 10) => new Sticker
            { id = "emoji_" + n, name = display, path = Emoji + "emoji_" + n + ".png", frames = frames, content = 124, fps = 12 };

        /// <summary>Every sticker. The order is baked into StickerPackOpener.textures — append only.</summary>
        public static readonly Sticker[] Stickers =
        {
            Emote("thumbsup", 10), Emote("laugh", 12), Emote("fire", 12), Emote("party", 12),
            Emote("cool", 10), Emote("wow", 10), Emote("cry", 12), Emote("angry", 10),
            Emote("thinking", 10), Emote("sleepy", 10),
            Emo("grinning", "GRIN"), Emo("fire", "BLAZE"), Emo("cool", "SHADES"), Emo("thinking", "HMMM"),
            Emo("clap", "CLAP"), Emo("party", "POPPER", 16), Emo("flex", "FLEX"), Emo("laughing", "LOL"),
        };

        private static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }

        public static readonly Pack[] Packs =
        {
            new Pack { id = "pack_classic", name = "CLASSIC PACK", rarity = "common", foil = Hex("#9A7148"), energyCost = 0,
                stickers = new[] { "emote_thumbsup", "emote_laugh", "emote_fire", "emote_party", "emote_cool", "emote_wow", "emote_cry", "emote_angry" } },
            // Placeholder costs until the owner prices them (energy packs start at 10 for $0.49).
            new Pack { id = "pack_gameface", name = "GAME FACE", rarity = "rare", foil = Hex("#1B89E7"), energyCost = 25,
                stickers = new[] { "emoji_laughing", "emoji_cool", "emoji_thinking", "emote_thinking", "emote_sleepy" } },
            new Pack { id = "pack_hype", name = "HYPE PACK", rarity = "epic", foil = Hex("#8E44D6"), energyCost = 40,
                stickers = new[] { "emoji_clap", "emoji_flex", "emoji_party", "emoji_fire", "emoji_grinning" } },
        };

        public static Sticker Find(string id)
        {
            foreach (var s in Stickers) if (s.id == id) return s;
            return null;
        }

        public static int IndexOf(string id)
        {
            for (int i = 0; i < Stickers.Length; i++) if (Stickers[i].id == id) return i;
            return -1;
        }

        // ======================================================================================
        // ownership
        // ======================================================================================

        private const string PrefKey = "ludonx_sticker_packs_v1";
        private static HashSet<string> _owned;

        private static HashSet<string> Owned
        {
            get
            {
                if (_owned != null) return _owned;
                _owned = new HashSet<string>();
                foreach (var id in PlayerPrefs.GetString(PrefKey, "").Split(','))
                    if (!string.IsNullOrEmpty(id)) _owned.Add(id);
                foreach (var p in Packs) if (p.energyCost == 0) _owned.Add(p.id);   // starters
                return _owned;
            }
        }

        internal static void ResetStatics() { _owned = null; }

        public static bool Owns(Pack pack) => pack != null && Owned.Contains(pack.id);

        public static bool OwnsSticker(string stickerId)
        {
            foreach (var p in Packs)
                if (Owns(p) && System.Array.IndexOf(p.stickers, stickerId) >= 0) return true;
            return false;
        }

        public enum BuyResult { Bought, AlreadyOwned, NotEnoughEnergy }

        /// <summary>Spends the energy and grants the pack. Nothing is spent unless it succeeds.</summary>
        public static BuyResult TryBuy(Pack pack)
        {
            if (Owns(pack)) return BuyResult.AlreadyOwned;
            if (!EnergyService.TrySpend(pack.energyCost)) return BuyResult.NotEnoughEnergy;
            Owned.Add(pack.id);
            PlayerPrefs.SetString(PrefKey, string.Join(",", Owned));
            PlayerPrefs.Save();
            return BuyResult.Bought;
        }

        /// <summary>Test helper: forget every bought pack (starters stay owned).</summary>
        public static void DebugReset()
        {
            PlayerPrefs.DeleteKey(PrefKey);
            _owned = null;
        }
    }
}
