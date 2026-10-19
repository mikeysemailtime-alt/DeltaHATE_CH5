using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using UndertaleModLib;

namespace DeltaHATE
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        #region some vars
        public UndertaleData Data { get; set; }
        public string FilePath { get; set; }
        public GameInfo Game { get; set; }
        public Random rng { get; set; }
        #endregion

        public int RandomSeed()
        {
            return new Random().Next(0, 1000000);
        }

        public bool EnsureDataLoaded()
        {
            return Data != null;
        }

        private void SetStatics()
        {
            Values.DoSprite = check_csprites.IsChecked == true;
            Values.DoText = check_cstrs.IsChecked == true;
            Values.DoSound = check_csnds.IsChecked == true;
            Values.DoFont = check_cfnts.IsChecked == true;
            Values.DoBackground = check_cbackground.IsChecked == true;

            // true = shuffle individual frames (stable), false = shuffle whole sprites (unstable)
            Values.SpriteType = radio_shufflebytpi.IsChecked == true;
        }

        private void ClearLoadedData()
        {
            Data = null;
            Game = null;
            FilePath = null;
            label_dataname.Content = "< Click to load!";
        }

        #region savenstuff
        private async Task DoOpenDialog()
        {
            OpenFileDialog dlg = new OpenFileDialog();

            dlg.DefaultExt = "win";
            dlg.Filter = "Game Maker Studio data files (.win, .unx, .ios)|*.win;*.unx;*.ios|All files|*";

            if (dlg.ShowDialog() == true)
            {
                await LoadFile(dlg.FileName);
            }
        }

        private async Task<bool> DoSaveDialog()
        {
            SaveFileDialog dlg = new SaveFileDialog();

            dlg.DefaultExt = "win";
            dlg.Filter = "Game Maker Studio data files (.win, .unx, .ios)|*.win;*.unx;*.ios|All files|*";
            dlg.InitialDirectory = Path.GetDirectoryName(FilePath);
            dlg.FileName = Path.GetFileName(FilePath);
            dlg.OverwritePrompt = true;

            if (dlg.ShowDialog() == true)
            {
                await SaveFile(dlg.FileName);
                return true;
            }
            return false;
        }

        private async Task LoadFile(string filename)
        {
            LoadDlg dialogue = new LoadDlg { Owner = this };

            UndertaleData data = null;
            string error = null;
            var warnings = new List<string>();

            Task t = Task.Run(() =>
            {
                try
                {
                    using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
                    {
                        // The warning handler gained an "isImportant" flag in newer UndertaleModLib versions.
                        data = UndertaleIO.Read(stream, (warning, isImportant) =>
                        {
                            if (isImportant)
                                lock (warnings) warnings.Add(warning);
                        });
                    }
                }
                catch (Exception e)
                {
                    error = e.Message;
                }

                Dispatcher.Invoke(() => dialogue.Close());
            });
            dialogue.ShowDialog();
            await t;

            if (error != null || data == null)
            {
                ClearLoadedData();
                MessageBox.Show("An error occured while trying to load:\n" + (error ?? "unknown error"),
                    "Load error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (warnings.Count > 0)
            {
                MessageBox.Show(string.Join("\n\n", warnings), "Loading warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            if (data.UnsupportedBytecodeVersion)
            {
                ClearLoadedData();
                MessageBox.Show("This game uses a bytecode version that UndertaleModLib doesn't support, so it can't be corrupted.",
                    "Unsupported game", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Data = data;
            FilePath = filename;
            Game = GameInfo.Detect(data, filename);
            label_dataname.Content = filename + "  [" + Game.Describe() + "]";
        }

        private async Task SaveFile(string filename)
        {
            if (Data == null || Data.UnsupportedBytecodeVersion) { return; }
            SetStatics();

            LoadDlg dialogue = new LoadDlg { Owner = this };
            CorruptReport report = null;
            string error = null;
            UndertaleData data = Data;
            GameInfo game = Game;
            string sourcePath = FilePath;
            Random random = rng;

            Task t = Task.Run(() =>
            {
                string tmp = filename + ".deltahate_tmp";
                try
                {
                    report = DoCorrupt.Run(data, sourcePath, random, game);

                    // Write to a temp file first so a failed write can't leave a half-written game file behind.
                    using (var stream = new FileStream(tmp, FileMode.Create))
                    {
                        UndertaleIO.Write(stream, data);
                    }

                    // If we're overwriting the file that was loaded, keep one untouched backup of it.
                    if (File.Exists(filename) && string.Equals(Path.GetFullPath(filename), Path.GetFullPath(sourcePath), StringComparison.OrdinalIgnoreCase))
                    {
                        string bak = filename + ".deltahate_backup";
                        if (!File.Exists(bak)) File.Copy(filename, bak);
                    }

                    File.Move(tmp, filename, true);
                }
                catch (Exception e)
                {
                    error = e.ToString();
                    try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
                }

                Dispatcher.Invoke(() => dialogue.Close());
            });
            dialogue.ShowDialog();
            await t;

            // The loaded data has been modified in memory either way; make the user load again
            // instead of silently stacking a second corruption on top.
            ClearLoadedData();

            if (error != null)
            {
                MessageBox.Show("Something went wrong while corrupting:\n\n" + error, "Egg?!?!?!?!??!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show("Corrupted succesfully! Probably!\n\n" + report +
                "\n\nA backup of the original was kept next to it as \"" + Path.GetFileName(filename) + ".deltahate_backup\" if you overwrote it." +
                "\nLoad the game again if you want to corrupt it once more.",
                "Egg?!?!?!?!??!", MessageBoxButton.OK, MessageBoxImage.None);
        }
        #endregion

        private async void button_loadfile_Click(object sender, RoutedEventArgs e)
        {
            await DoOpenDialog();
        }

        private void button_randomseed_Click(object sender, RoutedEventArgs e)
        {
            tbox_seedcamp.Text = RandomSeed().ToString();
        }

        private void lewindow_Loaded(object sender, RoutedEventArgs e)
        {
            tbox_seedcamp.Text = RandomSeed().ToString();
        }

        private async void button_corrupt_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureDataLoaded())
            {
                MessageBox.Show("Load the game first!", "Egg!!!!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!int.TryParse(tbox_seedcamp.Text, out int seed))
            {
                MessageBox.Show("The seed has to be a whole number (try the Random button).", "Egg!!!!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            rng = new Random(seed);

            if (Game != null && Game.IsChapterSelect)
            {
                var res = MessageBox.Show(
                    "This is the DELTARUNE Chapter Select (launcher) data.win - corrupting it won't change any chapter.\n\n" +
                    "Load the data.win inside the chapter folders instead (e.g. chapter1_windows\\data.win).\n\nCorrupt this one anyway?",
                    "Wrong data.win?", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (res != MessageBoxResult.Yes) return;
            }

            await DoSaveDialog();
        }

        private void quitter_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void minimize_bt_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void creditbt_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("DeltaHATE GUI version:\nby mike309\nChapter 5 functionality added by Claude AI\nOriginal DeltaHATE & UndertaleModLib:\nby krzys_h\nOriginal HATE:\nby RedSpah", "Egg", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ops_Click(object sender, RoutedEventArgs e)
        {
            NewOptions newopt = new NewOptions { Owner = this };
            newopt.ShowDialog();
        }

    }
}
