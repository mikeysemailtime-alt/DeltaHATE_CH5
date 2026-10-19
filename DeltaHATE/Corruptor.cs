--- DeltaHATE/Corruptor.cs
+++ DeltaHATE/Corruptor.cs
@@ -1, 10 +1, 10 @@
 using System;
 using System.Collections.Generic;
 using System.IO;
+using System.Linq;
 using System.Text.Json;
 using System.Text.RegularExpressions;
 using UndertaleModLib;
 
 namespace DeltaHATE
 {
     "tok-kw">public sealed "tok-kw">class CorruptReport
     {
         "tok-kw">public List<"tok-kw">string> Lines { get; } = "tok-kw">new List<"tok-kw">string>();
         "tok-kw">public "tok-kw">void Add("tok-kw">string line) { Lines.Add(line); }
         "tok-kw">public override "tok-kw">string ToString() { "tok-kw">return "tok-kw">string.Join(Environment.NewLine, Lines); }
     }
 
     "tok-kw">public "tok-kw">static "tok-kw">class Corruptor
     {
         "tok-kw">public "tok-kw">static CorruptReport Corrupt(UndertaleData data, GameInfo game, Random rng)
         {
             "tok-kw">var report = "tok-kw">new CorruptReport();
             
             "tok-kw">if (Values.DoSprite && data.Sprites != "tok-kw">null)
             {
                 // Sprite shuffling logic remains largely the same, but "tok-kw">ensure we handle large counts safely
                 "tok-kw">var indices = Enumerable.Range(0, data.Sprites.Count).ToList();
                 "tok-kw">if (indices.Count > 1)
                 {
                     indices.Shuffle(rng);
                     "tok-kw">for ("tok-kw">int i = 0; i < data.Sprites.Count; i++)
                     {
                         data.Sprites[i] = data.Sprites[indices[i]];
                     }
                     report.Add("Sprites shuffled: " + data.Sprites.Count);
                 }
             }
 
             "tok-kw">if (Values.DoSound && data.Sounds != "tok-kw">null)
             {
                 // Chapters ship several audio groups(e.g. audiogroup_default + audio_sfx "tok-kw">with an external
                 // audiogroup1.dat). Only swap sounds inside their own group so nothing ends up pointing at a
                 // group that isn&#039;t loaded when the game asks "tok-kw">for it.
                 
                 "tok-kw">var byGroup = "tok-kw">new Dictionary<"tok-kw">string, List<"tok-kw">int>>();
                 "tok-kw">for ("tok-kw">int i = 0; i < data.Sounds.Count; i++)
                 {
                     "tok-kw">var sound = data.Sounds[i];
                     "tok-kw">if (sound == "tok-kw">null) "tok-kw">continue;
                     
                     "tok-kw">string groupName = ""tok-kw">None";
                     "tok-kw">if (sound.AudioGroup != "tok-kw">null)
                     {
                         groupName = sound.AudioGroup.Name?.Content ?? "Unnamed";
                     }
                     
                     "tok-kw">if (!byGroup.ContainsKey(groupName))
                     {
                         byGroup[groupName] = "tok-kw">new List<"tok-kw">int>();
                     }
                     byGroup[groupName].Add(i);
                 }
                 
                 "tok-kw">int soundCount = 0;
                 "tok-kw">int shuffledCount = 0;
                 foreach("tok-kw">var kv "tok-kw">in byGroup)
                 {
                     soundCount += kv.Value.Count;
                     
                     // Only shuffle "tok-kw">if there are at least 2 sounds "tok-kw">in this group
                     "tok-kw">if (kv.Value.Count >= 2)
                     {
                         data.Sounds.ShuffleOnlySelected(kv.Value, rng);
                         shuffledCount += kv.Value.Count;
                     }
                     "tok-kw">else
                     {
                         report.Add($"Skipped audio group &#039;{kv.Key}&#039; (only {kv.Value.Count} sound(s))");
                     }
                 }
                 
                 report.Add("Sounds shuffled: " + soundCount + " ("tok-kw">in " + byGroup.Count + " audio group(s))");
             }
 
             "tok-kw">if (Values.DoFont && data.Fonts != "tok-kw">null)
             {
                 // Japanese "tok-kw">and other non-English fonts might have different glyph structures.
                 // We only shuffle fonts that have the same number of glyphs.
                 "tok-kw">var fontGroups = data.Fonts.GroupBy(f => f.Glyphs?.Count ?? 0).ToList();
                 
                 foreach("tok-kw">var group "tok-kw">in fontGroups)
                 {
                     "tok-kw">if (group.Count() > 1)
                     {
                         "tok-kw">var indices = Enumerable.Range(0, group.Count()).ToList();
                         indices.Shuffle(rng);
                         
                         "tok-kw">var fonts = group.ToList();
                         "tok-kw">for ("tok-kw">int i = 0; i < fonts.Count; i++)
                         {
                             fonts[i] = fonts[indices[i]];
                         }
                     }
                 }
             }
             
             "tok-kw">if (Values.DoText && data.Strings != "tok-kw">null && delta)
             {
                 // For Deltarune, we want to shuffle dialogue strings.
                 // In Chapter 1, these are "tok-kw">in lang_en.json. In Ch2-5, they are inline.
                 // We&#039;ll "tok-kw">try to find strings that look like dialogue.
                 
                 "tok-kw">var dialogueIndices = "tok-kw">new List<"tok-kw">int>();
                 "tok-kw">var nonDialogueIndices = "tok-kw">new List<"tok-kw">int>();
                 
                 // Common dialogue terminators "tok-kw">in Deltarune
                 "tok-kw">var dialogueEnders = "tok-kw">new[] { "\\M1", "\\M2", "\\M0", "\\M3", "\\M4", "\\M5", "\\M6", "\\M7", "\\M8", "\\M9" };
                 "tok-kw">bool hasLangFile = !"tok-kw">string.IsNullOrEmpty(game.LangFile);
                 
                 "tok-kw">for ("tok-kw">int i = 0; i < data.Strings.Count; i++)
                 {
                     "tok-kw">var str = data.Strings[i];
                     "tok-kw">if (str == "tok-kw">null || str.Content == "tok-kw">null) 
                     {
                         nonDialogueIndices.Add(i);
                         "tok-kw">continue;
                     }
                     
                     "tok-kw">string content = str.Content;
                     "tok-kw">bool isDialogue = "tok-kw">false;
                     
                     // 1. Check "tok-kw">for message box terminators(\M1, etc.)
                     foreach("tok-kw">var ender "tok-kw">in dialogueEnders)
                     {
                         "tok-kw">if (content.EndsWith(ender, StringComparison.Ordinal))
                         {
                             isDialogue = "tok-kw">true;
                             "tok-kw">break;
                         }
                     }
                     
                     // 2. If no terminator, check "tok-kw">if it&#039;s a short "tok-kw">string likely to be dialogue
                     // Long strings are often descriptions, script code, "tok-kw">or unused assets.
                     // Short strings might be variables "tok-kw">or UI labels.
                     // Dialogue "tok-kw">is typically between 5 "tok-kw">and 200 chars.
                     "tok-kw">if (!isDialogue && content.Length > 5 && content.Length < 200)
                     {
                         // Avoid shuffling very common short strings that might be keys "tok-kw">or IDs
                         "tok-kw">if (!content.StartsWith("gml_") && !content.StartsWith("scr_") && !content.Contains(" "))
                         {
                              // Heuristic: If it has spaces "tok-kw">and "tok-kw">is "tok-kw">not too short, it&#039;s likely dialogue
                              "tok-kw">if (content.Split(&#039; &#039;).Length > 1)
                              {
                                  isDialogue = "tok-kw">true;
                              }
                         }
                     }
                     
                     // 3. If we have a lang file, we can be more precise? 
                     // Actually, "tok-kw">for Ch4, we rely on the heuristic above.
                     
                     "tok-kw">if (isDialogue)
                     {
                         dialogueIndices.Add(i);
                     }
                     "tok-kw">else
                     {
                         nonDialogueIndices.Add(i);
                     }
                 }
                 
                 "tok-kw">if (dialogueIndices.Count > 1)
                 {
                     data.Strings.ShuffleOnlySelected(dialogueIndices, rng);
                     report.Add("Text shuffled: " + dialogueIndices.Count + " dialogue strings");
                 }
                 "tok-kw">else
                 {
                     report.Add("No dialogue strings found to shuffle. Text corruption skipped.");
                 }
                 
                 report.Add("Total strings scanned: " + data.Strings.Count + ", Non-dialogue preserved: " + nonDialogueIndices.Count);
             }
             
             "tok-kw">return report;
         }
     }
 }
