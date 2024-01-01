using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using FootballAppLib;
using System.Threading;
using System.Timers;
using System.Threading.Tasks;
using Windows.UI.ViewManagement;


namespace Unofficial_Football_Tournament_App
{
    /// <summary>
    /// Created by: Nadav Hilu
    /// Last Modified: 2023-09-10
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private static StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
        private static StorageFile currentFile;
        private static string fileContents = ""; //file format when split on "\n": "[0] tournament type\n [1] group stage teams in order\n [2] knock out teams in order [3] finalized"
        private static FOOTBALLDBAPI db;
        private static WebScraper scraper;
        private static string[] tournaments = { "Choose a tournament", "FIFA World Cup", "UEFA European Championship (N.Y.I)", "UEFA Champions League (N.Y.I)", "UEFA Europa League (N.Y.I)" };
        public const string appName = "FOOTBRAWL!";

        public static WebScraper Scraper { get => scraper; }
        public static StorageFile CurrentFile { get => currentFile; }
        public static string FileContents { get => fileContents; set { fileContents = value; } }
        public static FOOTBALLDBAPI DB { get => db; }
        public MainPage()
        {
            //MainPage Initialization
            this.InitializeComponent();
            if (scraper is null)
                InitializeScraper();
            
            GetSaveFiles();
            if (!(currentFile is null))
                GetFileContents();

            cboTournaments.ItemsSource = tournaments;
            cboTournaments.SelectedIndex = 0;
        }

        #region Initialization
        public void InitializeScraper() //Initializes WebScraper for gathering live data from website.
        {
            scraper = new WebScraper(new Uri(@"https://en.wikipedia.org/wiki/2022_FIFA_World_Cup"));

            while (scraper.ElementList is null || scraper.ElementList.Count == 0)
                Thread.Yield();

            if (scraper.ElementList is null || scraper.ElementList.Count == 0)
                InitializeScraper();
        }
        #endregion

        #region AppMethods
        public void WCGroupStageTransition() //Navigates to World Cup group stage
        {
            Frame.Navigate(typeof(WCGroupPage));
        }

        public void CreateTournament_Click(object sender, RoutedEventArgs e) //shows a pop-up for creating a new save file. on focus lost it disappears
        {
            int index = cboTournaments.SelectedIndex;
            switch (index)
            {
                case 0:
                    new MessageDialog("Please select a tournament from the dropdown list to continue", appName).ShowAsync();
                    break;
                case 1:
                    txtFileName.Visibility = Visibility.Visible;
                    txtFileName.Focus(FocusState.Keyboard);
                    break;
                default:
                    new MessageDialog("This tournament is unavailable at the moment", appName).ShowAsync();
                    break;
            }
        }

        public async void CreateFile(string fileName) //creates a .csv file based on the string parameter
        {
            try
            {
                currentFile = await storageFolder.CreateFileAsync($"{fileName}.csv", CreationCollisionOption.FailIfExists);
                if (cboTournaments.SelectedIndex == 1)
                {
                    FileIO.WriteTextAsync(currentFile, "World Cup\n").AsTask().Wait();
                    GetFileContents();
                    WCGroupStageTransition();
                }
            }
            catch (Exception)
            {
                new MessageDialog("A Saved file with that name already exists.\nPlease choose a different name", appName).ShowAsync();
            }
        }

        public async void ReadFile(string fileName) //reads a .csv file based on the string parameter
        {
            currentFile = null;
            try
            {
                currentFile = await storageFolder.GetFileAsync($"{fileName}.csv");
            }
            catch (Exception)
            {
                new MessageDialog("A problem occured while loading file", appName).ShowAsync();
            }
            while (currentFile is null)
                Thread.Yield();
            GetFileContents();
            if (FileContents.Split("\n")[0] == "World Cup")
            {
                WCGroupStageTransition();
            }

        }

        public async void GetSaveFiles() //retrieves all files from currentFolder
        {
            var saveFiles = await storageFolder.GetFilesAsync();
            lvwSaveFiles.Items.Clear();
            
            foreach (StorageFile file in saveFiles)
            {
                lvwSaveFiles.Items.Add(file.DisplayName);
            }

            if (lvwSaveFiles.Items.Count == 0)
                lvwSaveFiles.Items.Add("No Save Files Found :(");
        }
        public static async void GetFileContents() //reads file contents from currentFile
        {
            fileContents = "";
            Task<string> getFileContents = FileIO.ReadTextAsync(currentFile).AsTask();
            getFileContents.Wait();
            while (fileContents == "")
            {
                Thread.Yield();
                fileContents = getFileContents.Result;
            }
        }
        #endregion

        private void FileName_KeyUp(object sender, KeyRoutedEventArgs e) //on enter key pressed, validates file name and creates a file
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                if (txtFileName.Text == "" || txtFileName.Text is null)
                {
                    new MessageDialog("Save file must have a name", appName).ShowAsync();
                    return;
                }

                if (txtFileName.Text.All(c => Char.IsLetterOrDigit(c)))
                    ;
                else
                {
                    new MessageDialog("Invalid File name.\nFile name must contain letters and digits only", appName).ShowAsync();
                    return;
                }

                if (txtFileName.Text.Length > 25)
                {
                    new MessageDialog("Invalid File name.\nFile name must be 30 characters or less", appName).ShowAsync();
                    return;
                }
                CreateFile(txtFileName.Text);
            }
        }

        private void FileName_LostFocus(object sender, RoutedEventArgs e) //hides textbox on lost focus
        {
            txtFileName.Visibility = Visibility.Collapsed;
            txtFileName.Text = "";
        }

        private async void DeleteSaveFile_Click(object sender, RoutedEventArgs e) //deletes a save file after a confirmation
        {
            Button button = (Button)sender;
            if ((string)button.Content == "X")
            {
                if (button.Tag.ToString() == "No Save Files Found :(")
                    return;

                button.Content = "Confirm Deletion";
                button.Margin = new Thickness(300, 0, 0, 0);
                button.Width = 165;
                button.Focus(FocusState.Keyboard);
            }
            else if (button.Content == "Confirm Deletion")
            {
                button.Content = "X";
                button.Margin = new Thickness(430, 0, 0, 0);
                button.Width = 35;
                StorageFile savefile = await storageFolder.GetFileAsync(button.Tag.ToString() + ".csv");
                await savefile.DeleteAsync();
                GetSaveFiles();
            }
        }

        private void DeleteSaveFile_LostFocus(object sender, RoutedEventArgs e) //returns delete button back on lost focus
        {
            Button button = (Button)sender;
            button.Content = "X";
            button.Margin = new Thickness(430, 0, 0, 0);
            button.Width = 35;
        }

        private async void LoadSave_Click(object sender, RoutedEventArgs e) //uses user input to chose a file and executes Readfile();
        {
            string saveName = "";
            if (lvwSaveFiles.SelectedIndex != -1)
               saveName = lvwSaveFiles.SelectedItem.ToString();
            
            if (saveName == "No Save Files Found :(" || saveName == "" || saveName is null)
                return;
            else
                ReadFile(saveName);
        }
    }
}
