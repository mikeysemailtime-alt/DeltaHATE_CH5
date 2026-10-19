+++ DeltaHATE/Corruptor.cs
 using System;
 using System.Collections.Generic;
 using System.IO;
using System.Linq;
 using System.Text.Json;
 using System.Text.RegularExpressions;
 using UndertaleModLib;
 namespace DeltaHATE
 {
     public sealed class CorruptReport
    {
         public List<string> Lines { get; } = new List<string>();
         public void Add(string line) { Lines.Add(line); }
         public override string ToString() { return string.Join(Environment.NewLine, Lines); }
             }

             if (Values.DoSound && data.Sounds != null)
            {
                 // Chapters ship several audio groups (e.g. audiogroup_default + audio_sfx with an external
                 // audiogroup1.dat). Only swap sounds inside their own group so nothing ends up pointing at a
                 // group that isn't loaded when the game asks for it.
                     l.Add(i);
                 }
                 int soundCount = 0;
                int shuffledCount = 0;
                 foreach (var kv in byGroup)
                 {
                     soundCount += kv.Value.Count;
                    
                    // Only shuffle if there are at least 2 sounds in this group
                    if (kv.Value.Count >= 2)
                    {
                        data.Sounds.ShuffleOnlySelected(kv.Value, rng);
                        shuffledCount += kv.Value.Count;
                    }
                    else
                    {
                        report.Add($"Skipped audio group '{kv.Key}' (only {kv.Value.Count} sound(s))");
                    }
                 }
                
                 report.Add("Sounds shuffled: " + soundCount + " (in " + byGroup.Count + " audio group(s))");
            }

             if (Values.DoFont && data.Fonts != null)
             {
                 // Japanese and other non-English fonts might have different glyph structures.
                 // We only shuffle fonts that have the same number of glyphs.
                 var fontGroups = data.Fonts.GroupBy(f => f.Glyphs?.Count ?? 0).ToList();
                
                 foreach (var group in fontGroups)
                 {
                     if (group.Count() > 1)
                     }
                 }
             }
            
             if (Values.DoText && data.Strings != null && delta)
             {
                 // For Deltarune, we want to shuffle dialogue strings.
                 // In Chapter 1, these are in lang_en.json. In Ch2-5, they are inline.
                 // We'll try to find strings that look like dialogue.
                 
                var dialogueIndices = new List<int>();
                var nonDialogueIndices = new List<int>();
                
                // Common dialogue terminators in Deltarune
                var dialogueEnders = new[] { "\\M1", "\\M2", "\\M0", "\\M3", "\\M4", "\\M5", "\\M6", "\\M7", "\\M8", "\\M9" };
                bool hasLangFile = !string.IsNullOrEmpty(game.LangFile);
                
                for (int i = 0; i < data.Strings.Count; i++)
                 {
                    var str = data.Strings[i];
                    if (str == null || str.Content == null) 
                    {
                        nonDialogueIndices.Add(i);
                        continue;
                    }
                    
                    string content = str.Content;
                    bool isDialogue = false;
                    
                    // 1. Check for message box terminators (\M1, etc.)
                    foreach (var ender in dialogueEnders)
                     {
                        if (content.EndsWith(ender, StringComparison.Ordinal))
                         {
                             isDialogue = true;
                             break;
                         }
                     }
                     
                    // 2. If no terminator, check if it's a short string likely to be dialogue
                    // Long strings are often descriptions, script code, or unused assets.
                    // Short strings might be variables or UI labels.
                    // Dialogue is typically between 5 and 200 chars.
                    if (!isDialogue && content.Length > 5 && content.Length < 200)
                    {
                        // Avoid shuffling very common short strings that might be keys or IDs
                        if (!content.StartsWith("gml_") && !content.StartsWith("scr_") && !content.Contains(" "))
                        {
                             // Heuristic: If it has spaces and is not too short, it's likely dialogue
                             if (content.Split(' ').Length > 1)
                             {
                                 isDialogue = true;
                             }
                        }
                    }
                    
                    // 3. If we have a lang file, we can be more precise? 
                    // Actually, for Ch4, we rely on the heuristic above.
                    
                    if (isDialogue)
                    {
                        dialogueIndices.Add(i);
                    }
                    else
                    {
                        nonDialogueIndices.Add(i);
                    }
                 }
                 
                if (dialogueIndices.Count > 1)
                {
                    data.Strings.ShuffleOnlySelected(dialogueIndices, rng);
                    report.Add("Text shuffled: " + dialogueIndices.Count + " dialogue strings");
                }
                else
                {
                    report.Add("No dialogue strings found to shuffle. Text corruption skipped.");
                }
                
                report.Add("Total strings scanned: " + data.Strings.Count + ", Non-dialogue preserved: " + nonDialogueIndices.Count);
             }
             
             return report;

