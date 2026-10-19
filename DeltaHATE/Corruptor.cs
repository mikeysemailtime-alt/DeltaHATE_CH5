using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace DeltaHATE
{
    public sealed class CorruptReport
    {
        public List<string> Lines { get; } = new List<string>();
        public void Add(string line) { Lines.Add(line); }
        public override string ToString() { return string.Join(Environment.NewLine, Lines); }
    }

    public static class DoCorrupt
    {
        #region krzys' shuffle helpers (from UndertaleModTool's DeltaHATE.csx)
        static void Shuffle<T>(this IList<T> list, Random rng)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        static void ShuffleOnlySelected<T>(this IList<T> list, IList<int> selected, Action<int, int> swapFunc, Random rng)
        {
            int n = selected.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);

                swapFunc(selected[n], selected[k]);

                int idx = selected[k];
                selected[k] = selected[n];
                selected[n] = idx;
            }
        }

        static void ShuffleOnlySelected<T>(this IList<T> list, IList<int> selected, Random rng)
        {
            list.ShuffleOnlySelected(selected, (n, k) =>
            {
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }, rng);
        }
        #endregion

        static string SpriteName(UndertaleSprite s)
        {
            return s?.Name?.Content ?? "";
        }

        // "ftex_room_*" sprites (Chapter 5: ~800 of them) are baked room artwork, so they're handled like backgrounds.
        static bool IsBackgroundOrTiles(UndertaleSprite s)
        {
            string n = SpriteName(s);
            return n.StartsWith("bg_") || n.StartsWith("ftex_") || n.Contains("tiles");
        }

        static bool IsTiles(UndertaleSprite s)
        {
            return SpriteName(s).Contains("tiles");
        }

        static bool IsPlainBackground(UndertaleSprite s)
        {
            string n = SpriteName(s);
            return (n.StartsWith("bg_") || n.StartsWith("ftex_")) && !n.Contains("tiles");
        }

        /// <summary>Same size buckets UndertaleModTool's DeltaHATE.csx uses, so a 20x20 icon doesn't turn into a boss.</summary>
        static string SizeClass(UndertaleSprite s)
        {
            uint w = s.Width, h = s.Height;
            if (w < 50 && h < 50) return "tiny";
            if (w < 50 && h < 100) return "characterlike";
            if (w < 100 && h < 100) return "small";
            if (w < 200 && h < 200) return "big";
            return "huge";
        }

        public static CorruptReport Run(UndertaleData data, string filePath, Random rng, GameInfo game)
        {
            var report = new CorruptReport();
            bool delta = game.IsDeltarune;

            bool doSprites = Values.DoSprite;
            bool doBackgrounds = Values.DoBackground;
            bool stable = Values.SpriteType; // true = shuffle individual frames, false = whole sprites

            if (doSprites && data.Sprites != null)
            {
                if (stable) ShuffleSpriteFrames(data, delta, rng, report);
                else ShuffleWholeSprites(data, delta, rng, report);
            }

            if (doBackgrounds)
                ShuffleBackgrounds(data, delta, stable, rng, report);

            if (Values.DoSound && data.Sounds != null)
            {
                // Chapters ship several audio groups (e.g. audiogroup_default + audio_sfx with an external
                // audiogroup1.dat). Only swap sounds inside their own group so nothing ends up pointing at a
                // group that isn't loaded when the game asks for it.
                var byGroup = new Dictionary<string, List<int>>();
                for (int i = 0; i < data.Sounds.Count; i++)
                {
                    var snd = data.Sounds[i];
                    if (snd == null) continue;
                    string key = snd.AudioGroup?.Name?.Content ?? "<none>";
                    if (!byGroup.TryGetValue(key, out var l)) byGroup[key] = l = new List<int>();
                    l.Add(i);
                }
                int soundCount = 0;
                foreach (var kv in byGroup)
                {
                    soundCount += kv.Value.Count;
                    data.Sounds.ShuffleOnlySelected(kv.Value, rng);
                }
                report.Add("Sounds shuffled: " + soundCount + " (in " + byGroup.Count + " audio group(s))");
            }

            if (Values.DoFont && data.Fonts != null)
            {
                // Japanese and English fonts must stay in their own groups (as in UTMT's DeltaHATE.csx),
                // otherwise a language ends up rendered with a font that can't draw it.
                var ja = new List<int>();
                var other = new List<int>();
                for (int i = 0; i < data.Fonts.Count; i++)
                {
                    string n = data.Fonts[i]?.Name?.Content ?? "";
                    if (delta && n.StartsWith("fnt_ja_")) ja.Add(i); else other.Add(i);
                }
                data.Fonts.ShuffleOnlySelected(other, rng);
                data.Fonts.ShuffleOnlySelected(ja, rng);
                report.Add("Fonts shuffled: " + (ja.Count + other.Count));
            }

            if (Values.DoText)
            {
                if (delta) ShuffleDeltaruneText(data, game, rng, report);
                else report.Add("Text: skipped (only supported for DELTARUNE).");
            }

            return report;
        }

        #region sprites
        static void ShuffleSpriteFrames(UndertaleData data, bool delta, Random rng, CorruptReport report)
        {
            var pool = data.TexturePageItems;
            if (pool == null || pool.Count == 0) return;

            int count = 0;
            foreach (var sprite in data.Sprites)
            {
                if (sprite == null) continue;
                if (delta && IsBackgroundOrTiles(sprite)) continue;
                foreach (var tex in sprite.Textures)
                {
                    tex.Texture = pool[rng.Next(pool.Count)];
                    count++;
                }
            }
            report.Add("Sprite frames shuffled: " + count);
        }

        static void ShuffleWholeSprites(UndertaleData data, bool delta, Random rng, CorruptReport report)
        {
            // Objects (and rooms etc.) reference sprites as objects, not as list positions, so just swapping list
            // entries would change nothing for them. Remember which list slot each visible object's sprite sits in
            // *before* shuffling, then point the object at whatever ends up in that slot afterwards.
            var spriteSlot = new Dictionary<UndertaleSprite, int>(ReferenceEqualityComparer.Instance);
            for (int i = 0; i < data.Sprites.Count; i++)
                if (data.Sprites[i] != null) spriteSlot[data.Sprites[i]] = i;

            var objFixups = new List<(UndertaleGameObject obj, int sprite, int mask)>();
            if (data.GameObjects != null)
            {
                foreach (var obj in data.GameObjects)
                {
                    if (obj == null || !obj.Visible) continue;
                    int s = -1, m = -1;
                    if (obj.Sprite != null && spriteSlot.TryGetValue(obj.Sprite, out int si)) s = si;
                    if (obj.TextureMaskId != null && spriteSlot.TryGetValue(obj.TextureMaskId, out int mi)) m = mi;
                    if (s >= 0 || m >= 0) objFixups.Add((obj, s, m));
                }
            }

            // Pick what takes part. Group by size on DELTARUNE so shuffles stay (mostly) playable.
            var groups = new Dictionary<string, List<int>>();
            for (int i = 0; i < data.Sprites.Count; i++)
            {
                var sprite = data.Sprites[i];
                if (sprite == null) continue;
                string name = SpriteName(sprite);

                if (delta)
                {
                    if (!Values.DoKris && (name.StartsWith("spr_kris") || name.StartsWith("spr_dkris") || name.StartsWith("spr_mainchara")))
                        continue;
                    if (!Values.SuseiChalkScene && name.Contains("spr_susie_eatchalk"))
                        continue;
                    if (IsBackgroundOrTiles(sprite))
                        continue; // handled by the backgrounds option
                }

                string key = delta ? SizeClass(sprite) : "all";
                if (!groups.TryGetValue(key, out var list)) groups[key] = list = new List<int>();
                list.Add(i);
            }

            int count = 0;
            foreach (var kv in groups)
            {
                count += kv.Value.Count;
                data.Sprites.ShuffleOnlySelected(kv.Value, rng);
            }

            foreach (var (obj, s, m) in objFixups)
            {
                if (s >= 0) obj.Sprite = data.Sprites[s];
                if (m >= 0) obj.TextureMaskId = data.Sprites[m];
            }

            report.Add("Whole sprites shuffled: " + count + " (" + objFixups.Count + " objects re-pointed)");
        }
        #endregion

        #region backgrounds
        static void ShuffleBackgrounds(UndertaleData data, bool delta, bool stable, Random rng, CorruptReport report)
        {
            // GameMaker 2 tilesets live in the BGND chunk. Rooms reference them as objects, so swap the
            // graphics they point at instead of their list positions.
            if (data.Backgrounds != null && data.Backgrounds.Count > 1)
            {
                var textures = data.Backgrounds.Select(b => b?.Texture).ToList();
                textures.Shuffle(rng);
                for (int i = 0; i < data.Backgrounds.Count; i++)
                    if (data.Backgrounds[i] != null) data.Backgrounds[i].Texture = textures[i];
                report.Add("Tilesets shuffled: " + textures.Count);
            }

            if (!delta || data.Sprites == null) return;

            // DELTARUNE also draws backgrounds and tile graphics from "bg_*" sprites and "*tiles*" sprites.
            var bgs = data.Sprites.Where(s => s != null && IsPlainBackground(s) && s.Textures.Count > 0).ToList();
            var tiles = data.Sprites.Where(s => s != null && IsTiles(s) && s.Textures.Count > 0).ToList();

            if (stable)
            {
                // Re-point every frame at a random frame from the same family.
                var bgPool = bgs.Select(s => s.Textures[0].Texture).ToList();
                var tilePool = tiles.Select(s => s.Textures[0].Texture).ToList();
                foreach (var s in bgs)
                    foreach (var tex in s.Textures) tex.Texture = bgPool[rng.Next(bgPool.Count)];
                foreach (var s in tiles)
                    foreach (var tex in s.Textures) tex.Texture = tilePool[rng.Next(tilePool.Count)];
            }
            else
            {
                SwapFrameLists(bgs, rng);
                SwapFrameLists(tiles, rng);
            }
            report.Add("Background sprites shuffled: " + bgs.Count + ", tile sprites shuffled: " + tiles.Count);
        }

        /// <summary>Permutes the frame lists between sprites (whole-sprite look swaps places, sprite metadata stays).</summary>
        static void SwapFrameLists(List<UndertaleSprite> sprites, Random rng)
        {
            var lists = sprites.Select(s => s.Textures).ToList();
            lists.Shuffle(rng);
            for (int i = 0; i < sprites.Count; i++) sprites[i].Textures = lists[i];
        }
        #endregion

        #region text
        static readonly Regex ChoiceMarker = new Regex(@"\\+C(\d)", RegexOptions.Compiled);
        // Things like "lang/" or "mus/" end in a text-box control character but are not dialogue.
        // (Anything containing "..." is kept: "Heh.../" and ".../" are real lines.)
        static readonly Regex PathLike = new Regex(@"^[A-Za-z0-9_\-./\\:%]+$", RegexOptions.Compiled);

        /// <summary>
        /// Text boxes in DELTARUNE are driven by control characters at the end of a line (wait for input,
        /// continue, end, choices...). Lines must only be swapped with lines of the same kind, otherwise the
        /// game waits for input it can't get or reads a choice that isn't there (softlock).
        /// </summary>
        static string TextGroup(string str, bool includeUnmarked)
        {
            var choice = ChoiceMarker.Match(str);
            if (choice.Success)
            {
                // A bare "\C2 " string has no text of its own; it gets glued to a line by the game code. Leave it be.
                if (ChoiceMarker.Replace(str, "").Trim().Length == 0) return null;
                return "choice" + choice.Groups[1].Value;
            }
            if (str.EndsWith("/%%")) return "waiting_final";
            if (str.EndsWith("/%")) return "waiting_continue";
            if (str.EndsWith("/")) return "waiting";
            if (str.EndsWith("%%")) return "final";
            if (str.EndsWith("%")) return "continue";
            if (!includeUnmarked) return null;
            if (str.EndsWith("-")) return "dash";
            return "other";
        }

        static Dictionary<string, string> LoadLangFile(string path)
        {
            var result = new Dictionary<string, string>();
            using (var doc = JsonDocument.Parse(File.ReadAllText(path),
                new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip }))
            {
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (prop.Value.ValueKind != JsonValueKind.String) continue;
                    if (prop.Name == "date") continue;
                    string v = prop.Value.GetString();
                    if (v == "||") continue; // breaks the auto line break, no idea why it's localized
                    result[prop.Name] = v;
                }
            }
            return result;
        }

        static void ShuffleDeltaruneText(UndertaleData data, GameInfo game, Random rng, CorruptReport report)
        {
            if (data.Strings == null) return;

            var groups = new Dictionary<string, List<int>>();
            void Add(string key, int i)
            {
                if (!groups.TryGetValue(key, out var l)) groups[key] = l = new List<int>();
                l.Add(i);
            }

            if (game.LangFile != null)
            {
                // Chapter 1: the game code only holds keys, the text is in lang_en.json.
                // Swapping the keys swaps which text a line shows.
                var translations = LoadLangFile(game.LangFile);
                for (int i = 0; i < data.Strings.Count; i++)
                {
                    string id = data.Strings[i].Content;
                    if (id != null && translations.TryGetValue(id, out string text))
                    {
                        string g = TextGroup(text, true);
                        if (g != null) Add(g, i);
                    }
                }
                report.Add("Text: using " + Path.GetFileName(game.LangFile));
            }
            else
            {
                // Chapters 2+: English dialogue is stored inline in the data.win string table (the game calls
                // stringsetloc(english, key)), and there is no lang_en.json to tell dialogue from other strings,
                // so only lines that carry a text-box control character are touched.
                for (int i = 0; i < data.Strings.Count; i++)
                {
                    string s = data.Strings[i].Content;
                    if (s == null || s.Length <= 3) continue;
                    if (s.Any(c => c > 127)) continue;
                    if (PathLike.IsMatch(s) && !s.Contains("...")) continue;
                    string g = TextGroup(s, false);
                    if (g != null) Add(g, i);
                }
                report.Add("Text: no lang_en.json for this game, shuffling inline English text.");
            }

            void StringSwap(int n, int k)
            {
                // Swap the contents because UndertaleModTool dedupes strings by object.
                string value = data.Strings[k].Content;
                data.Strings[k].Content = data.Strings[n].Content;
                data.Strings[n].Content = value;
            }

            int total = 0;
            foreach (var kv in groups)
            {
                total += kv.Value.Count;
                data.Strings.ShuffleOnlySelected(kv.Value, StringSwap, rng);
            }
            report.Add("Text lines shuffled: " + total);
        }
        #endregion
    }
}
