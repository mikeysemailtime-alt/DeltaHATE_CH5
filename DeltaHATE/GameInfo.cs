+++ DeltaHATE/GameInfo.cs
 using System;
 using System.IO;
using System.Collections.Generic;
 using System.Text.RegularExpressions;
 using UndertaleModLib;

             }

             if (info.IsDeltarune && dir != null)
            {
                 string langDir = Path.Combine(dir, "lang");
                 var candidates = new System.Collections.Generic.List<string> { "lang_en.json" };
                 if (info.Chapter.HasValue) candidates.Add("lang_en_ch" + info.Chapter.Value + ".json");
                 {
                     string p = Path.Combine(langDir, c);
                     if (File.Exists(p)) { info.LangFile = p; break; }
                 }
                
                // Explicitly set LangFile to null if no English file exists, 
                // so the Corruptor knows to use heuristic-based shuffling.
                if (info.LangFile == null && Directory.Exists(langDir)) { /* Already null, but explicit */ }
             }

             return info;

