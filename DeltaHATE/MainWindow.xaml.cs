+++ DeltaHATE/MainWindow.xaml.cs
 using System;
 using System.Collections.Generic;
using System.Diagnostics;
 using System.IO;
 using System.Threading.Tasks;
 using System.Windows;
             Data = data;
             FilePath = filename;
             Game = GameInfo.Detect(data, filename);
            
            // Log some debug info for Chapter 4 detection
            Debug.WriteLine($"Detected Game: {Game.Describe()}, LangFile: {Game.LangFile ?? "None"}");
            
             label_dataname.Content = filename + "  [" + Game.Describe() + "]";
         }

