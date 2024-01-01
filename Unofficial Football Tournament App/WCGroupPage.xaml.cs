using FootballAppLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
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


namespace Unofficial_Football_Tournament_App
{
    /// <summary>
    /// Created by: Nadav Hilu
    /// Last Modified: 2023-09-09
    /// </summary>
    public sealed partial class WCGroupPage : Page
    {
        private static WebScraper s = MainPage.Scraper;
        private static StorageFile currentFile = MainPage.CurrentFile;
        private static FOOTBALLDBAPI db = MainPage.DB;
        private static List<TEAM> teams = new List<TEAM>();


        public WCGroupPage()
        {

            currentFile = MainPage.CurrentFile;
            db = MainPage.DB;
            if (db is null)
                db = InitializeDB(db);

            this.InitializeComponent();
            PopulateGroupStage();
            MainPage.GetFileContents();
            if (!(db.GetTeams().Count == 0 || db.GetTeams() is null))
                WCUpdateTeams();
            else
                WCPopulateTeams();

            if (!(db.GetMatches().Count == 0 || db.GetMatches() is null))
                WCUpdateMatches();
            else
                WCPopulateMatches();

            txtSaveName.Text = "Current save: " + currentFile.DisplayName.Replace(".csv", "");

            Finalized();
        }

        #region Initialization
        public static FOOTBALLDBAPI InitializeDB(FOOTBALLDBAPI db) //Initializes connection to database. Creates database on first time startup
        {
            try
            {
                db = new FOOTBALLDBAPI(App.ConnStr);
            }
            catch (ArgumentException ex)
            {
                new MessageDialog(ex.Message).ShowAsync();
            }
            catch (Exception)
            {
                new MessageDialog("A problem occured while establishing connection to database", MainPage.appName).ShowAsync();
            }

            return db;
        }
        #endregion

        #region ScraperMethods
        internal static List<TEAM> WCDownloadTeams() //Retrieves teams from website
        {
            List<TEAM> updatedTeams = new List<TEAM>();
            List<string> teams = WebScraper.GetContent(WebScraper.GetContent(s.ElementList, "th scope=\"row\" style=\"text-align: left; white-space:nowrap;font-weight: normal;background-color"), "a").Distinct().ToList();
            List<string> teamNums = WebScraper.GetContent(WebScraper.GetContent(s.ElementList, "tbody"), "td style=\"font-weight").Where(x => x.All(c => !(Char.IsLetter(c)))).ToList();
            for (int i = 0; i < teamNums.Count - 1; i++)
            {
                if (teamNums[i].StartsWith("&"))
                    teamNums[i] = "-" + teamNums[i].Substring(teamNums[i].Length - 2, 1);
            }

            int groupCount = 0;
            char group = 'A';
            foreach (string team in teams)
            {
                TEAM updatedTeam = new TEAM(team, int.Parse(teamNums[1]), int.Parse(teamNums[3]), int.Parse(teamNums[2]), int.Parse(teamNums[4]) - int.Parse(teamNums[5]), int.Parse(teamNums[7]), group.ToString());
                updatedTeams.Add(updatedTeam);
                teamNums.RemoveRange(0, 8);
                groupCount++;
                if (groupCount == 4)
                {
                    groupCount = 0;
                    group++;
                }
            }
            return updatedTeams;
        }

        internal static List<MATCH> WCDownloadMatches() //Retrieves matches from website
        {
            List<MATCH> updatedMatches = new List<MATCH>();
            List<string> teams = WebScraper.GetContent(WebScraper.GetContent(s.ElementList, "span itemprop=\"name\""), "a href=\"/wiki/");
            List<string> teamNums = WebScraper.GetContent(WebScraper.GetContent(WebScraper.GetContent(s.ElementList, "tr itemprop=\"name\""), "th class=\"fscore\""), "a href=\"/wiki/2022");
            List<string> playersScored = WebScraper.GetContent(WebScraper.GetContent(s.ElementList, "tr class=\"fgoals\""), "a href=\"/wiki/");
            List<string> playersScoredList = WebScraper.GetContent(WebScraper.GetContent(s.ElementList, "tr class=\"fgoals\""), "li");

            //For loops to clean data
            for (int i = 0; i < playersScored.Count - 1; i++)
                if (playersScored[i] == "pen." || playersScored[i] == "o.g.")
                {
                    playersScored.Remove(playersScored[i]);
                    i--;
                }
            for (int i = 0; i < playersScoredList.Count - 1; i++)
                if (!(playersScoredList[i].Contains("&#39")) && !(playersScored.Any(x => x == playersScoredList[i])))
                {
                    playersScoredList.Remove(playersScoredList[i]);
                    i--;
                }/*close this line with / for easier debugging*
                else if (playersScoredList[i].Contains("&"))
                {
                    playersScoredList[i] = playersScoredList[i].Replace("&#39", "'");
                    playersScoredList[i] = playersScoredList[i].Split("'")[0];
                }
                /**/

            //foreach loop to create single matches using parallel lists
            foreach (string nums in new List<string>(teamNums))
            {
                string[] scores = nums.Split("–");
                int tOneScore = int.Parse(scores[0]);
                int tTwoScore = int.Parse(scores[1]);
                string playersScoredString = "";

                for (int i = tOneScore + tTwoScore; i > 0;)
                {
                    string player = playersScoredList[0];
                    playersScoredList.Remove(playersScoredList[0]);
                    while (playersScoredList[0].Any(c => Char.IsDigit(c)) && i > 0)
                    {
                        playersScoredString += player + ",";
                        i--;
                        playersScoredList.Remove(playersScoredList[0]);
                    }
                }
                //removes hangging comma
                if (playersScoredString.Length > 0)
                    playersScoredString = playersScoredString.Substring(0, playersScoredString.Length - 1);

                updatedMatches.Add(new MATCH(teams[0], teams[1], tOneScore, tTwoScore, playersScoredString));
                teamNums.Remove(nums);
                teams.RemoveRange(0, 2);
            }
            return updatedMatches;
        }
        #endregion

        #region DBQueries
        internal static void WCPopulateTeams() //First Time DB populating
        {
            List<TEAM> updatedTeams = WCDownloadTeams();
            try
            {
                foreach (TEAM team in updatedTeams)
                    db.InsertTeam(team);
            }
            catch (Exception ex)
            {
                new MessageDialog("Database could not be reached. Restart application, or continue without database");
            }
        }

        internal static void WCPopulateMatches() //First Time DB populating
        {
            List<MATCH> updatedMatches = WCDownloadMatches();
            try
            {
                foreach (MATCH match in updatedMatches)
                    db.InsertMatch(match);
            }
            catch (Exception ex)
            {
                new MessageDialog("Database could not be created on startup. Restart application, or continue without database");
            }
        }

        internal static void WCUpdateTeams() //Updates DB with results
        {
            List<TEAM> updatedTeams = WCDownloadTeams();
            try
            {
                foreach (TEAM team in updatedTeams)
                    db.UpdateTeam(team);
            }
            catch (Exception ex)
            {
                new MessageDialog("Database could not be updated on startup. Restart application, or continue without database");
            }
        }

        internal static void WCUpdateMatches() //Updates DB with results
        {
            List<MATCH> updatedMatches = WCDownloadMatches();
            try
            {
                foreach (MATCH match in updatedMatches)
                    db.UpdateMatch(match);
            }
            catch (Exception ex)
            {
                new MessageDialog("Database could not be updated on startup. Restart application, or continue without database");
            }
        }
        #endregion

        #region UIMethods
        public async void PopulateGroupStage() //populates group stage listviews with teams
        {
            var groups = grid.Children.Where(obj => obj is ListView).ToList();

            if (teams.Count != 32)
                teams = WCDownloadTeams();

            if (MainPage.FileContents == "World Cup\n") //new save file
            {
                for (int i = groups.Count - 1; i >= 0; i--)
                {
                    ListView lvw = (ListView)groups[i];
                    lvw.ItemsSource = new ObservableCollection<string>(teams.Where(team => team.teamGroup == lvw.Name)
                                                                            .Select(team => $"{team.teamName}")
                                                                            .ToList());
                }
            }
            else //already configured save file that needs to be read
            {
                Queue<string> savedTeams = new Queue<string>(MainPage.FileContents.Split("\n")[1].Split(","));

                for (int i = groups.Count - 1; i >= 0; i--)
                {
                    ListView lvw = (ListView)groups[i];
                    List<TEAM> groupedteams = new List<TEAM>();

                    foreach (string s in savedTeams)
                        foreach (TEAM t in teams)
                            if (s == t.teamName && t.teamGroup == lvw.Name)
                                groupedteams.Add(t);

                    lvw.ItemsSource = new ObservableCollection<string>(groupedteams.Select(team => team.teamName));
                }
            }
        }

        private void Finalized() //checks if save file has been finalized. Finalization locks user interaction.
        {
            if (MainPage.FileContents.Split("\n").Length == 4 && MainPage.FileContents.Split("\n")[3] == "Finalized")
            {
                var groups = grid.Children.Where(obj => obj is ListView).Select(obj => obj as ListView).ToList();

                foreach (ListView lvw in groups)
                {
                    lvw.IsEnabled = false;
                }
            }
        }
        #endregion   
        private void MainMenu_Click(object sender, RoutedEventArgs e) //nagivates to MainMenu page
        {
            Frame.Navigate(typeof(MainPage));
        }

        private async void ContinueToKnockOut_Click(object sender, RoutedEventArgs e) //saves group stage team order and loads knockout stage page
        {
            string output = MainPage.FileContents.Split("\n")[0] + "\n";
            var groups = grid.Children.Where(obj => obj is ListView).Select(obj => obj as ListView).ToList();

            foreach (ListView list in groups)
                foreach (string s in list.Items)
                    output += $"{s},";

            output += "\n";

            if (MainPage.FileContents.Split("\n").Length >= 3)
                output += MainPage.FileContents.Split("\n")[2] + "\n";
            if (MainPage.FileContents.Split("\n").Length == 4)
                output += MainPage.FileContents.Split("\n")[3];

            await FileIO.WriteTextAsync(currentFile, output);

            MainPage.FileContents = output;
            Frame.Navigate(typeof(WCKockOut));
        }
    }
}
