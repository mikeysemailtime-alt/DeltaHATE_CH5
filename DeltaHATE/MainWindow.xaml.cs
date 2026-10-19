--- DeltaHATE/MainWindow.xaml.cs
+++ DeltaHATE/MainWindow.xaml.cs
@@ -1, 10 +1, 10 @@
 using System;
 using System.Collections.Generic;
+using System.Diagnostics;
 using System.IO;
 using System.Threading.Tasks;
 using System.Windows;
 using System.Windows.Controls;
 using System.Windows.Media;
 using UndertaleModLib;
 
 namespace DeltaHATE
 {
     /// <summary>
     /// Interaction logic "tok-kw">for MainWindow.xaml
     /// </summary>
     "tok-kw">public partial "tok-kw">class MainWindow : Window
     {
         "tok-kw">private UndertaleData Data;
         "tok-kw">private "tok-kw">string FilePath;
         "tok-kw">private GameInfo Game;
 
         "tok-kw">public MainWindow()
         {
             InitializeComponent();
         }
 
         "tok-kw">private "tok-kw">async "tok-kw">void button_loadfile_Click(object sender, RoutedEventArgs e)
         {
             "tok-kw">var dialog = "tok-kw">new Microsoft.Win32.OpenFileDialog();
             dialog.Filter = "Data Files(.dat .win)|.dat;.win|All Files(.)|.";
             "tok-kw">if (dialog.ShowDialog() == "tok-kw">true)
             {
                 "tok-kw">await LoadFileAsync(dialog.FileName);
             }
         }
 
         "tok-kw">private "tok-kw">async Task LoadFileAsync("tok-kw">string filename)
         {
             "tok-kw">try
             {
                 LoadDlg loadDlg = "tok-kw">new LoadDlg();
                 loadDlg.Show();
                 
                 "tok-kw">await Task.Run(() =>
                 {
                     Data = UndertaleLoader.Load(filename);
                 });
                 
                 Game = GameInfo.Detect(Data, filename);
                 
                 // Log some debug info "tok-kw">for Chapter 4 detection
                 Debug.WriteLine($"Detected Game: {Game.Describe()}, LangFile: {Game.LangFile ?? ""tok-kw">None"}");
                 
                 label_dataname.Content = filename + "  [" + Game.Describe() + "]";
                 
                 // Enable buttons
                 button_corrupt.IsEnabled = "tok-kw">true;
                 
                 loadDlg.Close();
             }
             "tok-kw">catch (Exception ex)
             {
                 MessageBox.Show($"Error loading file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
             }
         }
 
         "tok-kw">private "tok-kw">async "tok-kw">void button_corrupt_Click(object sender, RoutedEventArgs e)
         {
             "tok-kw">if (Data == "tok-kw">null) "tok-kw">return;
             
             "tok-kw">try
             {
                 LoadDlg loadDlg = "tok-kw">new LoadDlg();
                 loadDlg.label.Content = "Corrupting...";
                 loadDlg.Show();
                 
                 "tok-kw">var rng = "tok-kw">new Random();
                 "tok-kw">if ("tok-kw">int.TryParse(tbox_seedcamp.Text, out "tok-kw">int seed))
                 {
                     rng = "tok-kw">new Random(seed);
                 }
                 
                 "tok-kw">var report = "tok-kw">await Task.Run(() => Corruptor.Corrupt(Data, Game, rng));
                 
                 loadDlg.Close();
                 
                 // Save the corrupted file
                 "tok-kw">var saveDialog = "tok-kw">new Microsoft.Win32.SaveFileDialog();
                 saveDialog.FileName = Path.GetFileName(FilePath);
                 saveDialog.Filter = "Data Files(.dat .win)|.dat;.win|All Files(.)|.";
                 
                 "tok-kw">if (saveDialog.ShowDialog() == "tok-kw">true)
                 {
                     "tok-kw">try
                     {
                         // Create backup
                         File.Copy(FilePath, FilePath + ".deltahate_backup", "tok-kw">true);
                         
                         UndertaleWriter.WriteFile(saveDialog.FileName, Data);
                         MessageBox.Show($"Saved to {saveDialog.FileName}\n\nReport:\n{report}", "Done!", MessageBoxButton.OK, MessageBoxImage.Information);
                     }
                     "tok-kw">catch (Exception ex)
                     {
                         MessageBox.Show($"Error saving file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                     }
                 }
             }
             "tok-kw">catch (Exception ex)
             {
                 MessageBox.Show($"Error corrupting file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
             }
         }
 
         "tok-kw">private "tok-kw">void quitter_Click(object sender, RoutedEventArgs e)
         {
             Application.Current.Shutdown();
         }
 
         "tok-kw">private "tok-kw">void minimize_bt_Click(object sender, RoutedEventArgs e)
         {
             this.WindowState = WindowState.Minimized;
         }
 
         "tok-kw">private "tok-kw">void button_randomseed_Click(object sender, RoutedEventArgs e)
         {
             tbox_seedcamp.Text = "tok-kw">new Random().Next().ToString();
         }
 
         "tok-kw">private "tok-kw">void lewindow_Loaded(object sender, RoutedEventArgs e)
         {
             // Initialize values "tok-kw">from checkboxes
             Values.DoSprite = check_csprites.IsChecked ?? "tok-kw">true;
             Values.DoBackground = check_cbackground.IsChecked ?? "tok-kw">false;
             Values.DoSound = check_csnds.IsChecked ?? "tok-kw">true;
             Values.DoText = check_cstrs.IsChecked ?? "tok-kw">true;
             Values.DoFont = check_cfnts.IsChecked ?? "tok-kw">false;
             Values.SpriteType = radio_shufflebytpi.IsChecked ?? "tok-kw">false;
         }
     }
 }
