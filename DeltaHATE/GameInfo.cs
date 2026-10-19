using System;
using System.IO;
using System.Text.RegularExpressions;
using UndertaleModLib;

namespace DeltaHATE
{
    /// <summary>
    /// Works out what kind of data.win was loaded.
    ///
    /// Current DELTARUNE ships as a launcher ("Chapter Select") plus one folder per chapter:
    ///   DELTARUNE\data.win                       - chapter select / launcher
    ///   DELTARUNE\chapter1_windows\data.win      - chapter 1  (has lang\lang_en.json)
    ///   DELTARUNE\chapter2_windows\data.win      - chapter 2+ (English text is inline in the game code,
    ///                                                          only lang\lang_ja.json is shipped)
    /// </summary>
    public sealed class GameInfo
    {
        public bool IsDeltarune { get; private set; }

        /// <summary>The launcher data.win. Shuffling it does nothing useful for the chapters.</summary>
        public bool IsChapterSelect { get; private set; }

        /// <summary>Chapter number if it could be told from the folder name, otherwise null.</summary>
        public int? Chapter { get; private set; }

        /// <summary>Full path of an English lang json (chapter 1), or null if the game has none.</summary>
        public string LangFile { get; private set; }

        public string DisplayName { get; private set; } = "";

        public string Describe()
        {
            if (!IsDeltarune) return "GameMaker game";
            if (IsChapterSelect) return "DELTARUNE Chapter Select (launcher)";
            return Chapter.HasValue ? "DELTARUNE Chapter " + Chapter.Value : "DELTARUNE";
        }

        public static GameInfo Detect(UndertaleData data, string filePath)
        {
            var info = new GameInfo();

            string display = data.GeneralInfo?.DisplayName?.Content ?? "";
            string name = data.GeneralInfo?.Name?.Content ?? "";
            info.DisplayName = display;

            string dir = null;
            try { dir = Path.GetDirectoryName(Path.GetFullPath(filePath)); } catch { }
            string folder = dir == null ? "" : Path.GetFileName(dir) ?? "";

            var folderMatch = Regex.Match(folder, @"^chapter(\d+)", RegexOptions.IgnoreCase);
            bool inChapterFolder = folderMatch.Success;

            // Same conditions UndertaleModTool's own deltarune game definition uses.
            bool byName = Regex.IsMatch(display, @"^SURVEY_PROGRAM$|^deltarune", RegexOptions.IgnoreCase)
                       || Regex.IsMatch(name, @"^deltarune", RegexOptions.IgnoreCase);

            // The old check (kept as a fallback for old/modded builds).
            bool legacy = data.Scripts?.ByName("scr_debug")?.Code != null;

            info.IsDeltarune = byName || legacy || inChapterFolder;

            if (inChapterFolder && int.TryParse(folderMatch.Groups[1].Value, out int ch))
                info.Chapter = ch;
            else
            {
                // Current builds name themselves e.g. "DELTARUNE Chapter 5".
                var dm = Regex.Match(display, @"chapter\s*(\d+)", RegexOptions.IgnoreCase);
                if (dm.Success && int.TryParse(dm.Groups[1].Value, out int dch)) info.Chapter = dch;
            }

            if (info.IsDeltarune && !inChapterFolder && dir != null
                && Directory.Exists(Path.Combine(dir, "chapter1_windows")))
            {
                info.IsChapterSelect = true;
            }

            if (info.IsDeltarune && dir != null)
            {
                string langDir = Path.Combine(dir, "lang");
                var candidates = new System.Collections.Generic.List<string> { "lang_en.json" };
                if (info.Chapter.HasValue) candidates.Add("lang_en_ch" + info.Chapter.Value + ".json");
                candidates.Add("lang_en_ch1.json"); // older chapter 1 / demo builds
                foreach (string c in candidates)
                {
                    string p = Path.Combine(langDir, c);
                    if (File.Exists(p)) { info.LangFile = p; break; }
                }
            }

            return info;
        }
    }
}
