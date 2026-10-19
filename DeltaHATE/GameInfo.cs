--- DeltaHATE/GameInfo.cs
+++ DeltaHATE/GameInfo.cs
@@ -1, 10 +1, 10 @@
 using System;
 using System.IO;
+using System.Collections.Generic;
 using System.Text.RegularExpressions;
 using UndertaleModLib;
 
 namespace DeltaHATE
 {
     "tok-kw">public "tok-kw">class GameInfo
     {
         "tok-kw">public "tok-kw">string Name { get; set; }
         "tok-kw">public "tok-kw">string DisplayName { get; set; }
         "tok-kw">public "tok-kw">int? Chapter { get; set; }
         "tok-kw">public "tok-kw">bool IsDeltarune { get; set; }
         "tok-kw">public "tok-kw">string LangFile { get; set; }
         
         "tok-kw">public "tok-kw">string Describe()
         {
             "tok-kw">if (IsDeltarune)
             {
                 "tok-kw">if (Chapter.HasValue)
                     "tok-kw">return $"DELTARUNE Chapter {Chapter.Value}";
                 "tok-kw">return "DELTARUNE";
             }
             "tok-kw">return Name ?? "Unknown Game";
         }
 
         "tok-kw">public "tok-kw">static GameInfo Detect(UndertaleData data, "tok-kw">string filename)
         {
             "tok-kw">var info = "tok-kw">new GameInfo();
             
             // Basic detection "tok-kw">from UndertaleModLib
             info.Name = data.GeneralInfo?.GameName?.Content ?? "Unknown";
             info.DisplayName = data.GeneralInfo?.GameDisplayName?.Content ?? info.Name;
             
             // Check "tok-kw">for Deltarune specific markers
             "tok-kw">if (info.Name.Contains("DELTARUNE") || info.DisplayName.Contains("DELTARUNE"))
             {
                 info.IsDeltarune = "tok-kw">true;
                 
                 // Try to detect chapter "tok-kw">from filename "tok-kw">or internal data
                 // Filename pattern: chapter1_windows, chapter2_windows, etc.
                 "tok-kw">var dir = Path.GetDirectoryName(filename);
                 "tok-kw">if (dir != "tok-kw">null)
                 {
                     "tok-kw">var dirName = Path.GetFileName(dir);
                     "tok-kw">if (dirName.StartsWith("chapter", StringComparison.OrdinalIgnoreCase))
                     {
                         "tok-kw">if ("tok-kw">int.TryParse(dirName.Substring(7), out "tok-kw">int ch))
                         {
                             info.Chapter = ch;
                         }
                     }
                 }
                 
                 // Also check "tok-kw">if there&#039;s a specific chapter marker "tok-kw">in the game data "tok-kw">if available
                 // For now, we rely on the folder name "tok-kw">as it&#039;s most reliable "tok-kw">for modern DELTARUNE
             }
 
             "tok-kw">if (info.IsDeltarune && dir != "tok-kw">null)
             {
                 "tok-kw">string langDir = Path.Combine(dir, "lang");
                 "tok-kw">var candidates = "tok-kw">new List<"tok-kw">string> { "lang_en.json" };
                 "tok-kw">if (info.Chapter.HasValue) candidates.Add("lang_en_ch" + info.Chapter.Value + ".json");
                 
                 foreach("tok-kw">var c "tok-kw">in candidates)
                 {
                     "tok-kw">string p = Path.Combine(langDir, c);
                     "tok-kw">if (File.Exists(p)) { info.LangFile = p; "tok-kw">break; }
                 }
                 
                 // Explicitly set LangFile to "tok-kw">null "tok-kw">if no English file exists, 
                 // so the Corruptor knows to "tok-kw">use heuristic-based shuffling.
                 "tok-kw">if (info.LangFile == "tok-kw">null && Directory.Exists(langDir)) { / Already "tok-kw">null, but explicit / }
             }
 
             "tok-kw">return info;
         }
     }
 }
