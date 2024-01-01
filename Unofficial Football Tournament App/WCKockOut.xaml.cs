using FootballAppLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Xml.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using static System.Net.Mime.MediaTypeNames;


namespace Unofficial_Football_Tournament_App
{
    /// <summary>
    /// Created by: Nadav Hilu
    /// Last Modified: 2023-09-09
    /// </summary>
    public sealed partial class WCKockOut : Page
    {
        private static WebScraper s = MainPage.Scraper;
        private static StorageFile currentFile = MainPage.CurrentFile;
        private static FOOTBALLDBAPI db = MainPage.DB;
        private static List<TEAM> teams = WCGroupPage.WCDownloadTeams();
        private static string fileContents = MainPage.FileContents; //file format when split on "\n": "[0] tournament type\n [1] group stage teams in order\n [2] knock out teams in order [3] finalized"

        public WCKockOut()
        {
            MainPage.GetFileContents();
            currentFile = MainPage.CurrentFile;
            fileContents = MainPage.FileContents;
            db = MainPage.DB;
            this.InitializeComponent();
            string[] teams = fileContents.Split("\n")[1].Split(",");


            /*********************************************************************************************************************************
            *                                                                                                                                *
            *  FUTURE UPDATE: Instead of brute force placement a method will be added to check save finalization and load data accordingly   *  (N.Y.I)
            *                                                                                                                                *
            **********************************************************************************************************************************/
            //Round of 16
            txtA1.Text = teams[0];
            txtA2.Text = teams[1];
            txtB1.Text = teams[4];
            txtB2.Text = teams[5];
            txtC1.Text = teams[8];
            txtC2.Text = teams[9];
            txtD1.Text = teams[12];
            txtD2.Text = teams[13];
            txtE1.Text = teams[16];
            txtE2.Text = teams[17];
            txtF1.Text = teams[20];
            txtF2.Text = teams[21];
            txtG1.Text = teams[24];
            txtG2.Text = teams[25];
            txtH1.Text = teams[28];
            txtH2.Text = teams[29];


            //quarter finals left side
            cboQFL1.Items.Add(teams[0]);
            cboQFL1.Items.Add(teams[5]);
            cboQFL1.SelectedIndex = 0;

            cboQFL2.Items.Add(teams[8]);
            cboQFL2.Items.Add(teams[13]);
            cboQFL2.SelectedIndex = 0;

            cboQFL3.Items.Add(teams[16]);
            cboQFL3.Items.Add(teams[21]);
            cboQFL3.SelectedIndex = 0;

            cboQFL4.Items.Add(teams[24]);
            cboQFL4.Items.Add(teams[29]);
            cboQFL4.SelectedIndex = 0;

            //quarter finals right side
            cboQFR1.Items.Add(teams[4]);
            cboQFR1.Items.Add(teams[1]);
            cboQFR1.SelectedIndex = 0;

            cboQFR2.Items.Add(teams[12]);
            cboQFR2.Items.Add(teams[9]);
            cboQFR2.SelectedIndex = 0;

            cboQFR3.Items.Add(teams[20]);
            cboQFR3.Items.Add(teams[17]);
            cboQFR3.SelectedIndex = 0;

            cboQFR4.Items.Add(teams[28]);
            cboQFR4.Items.Add(teams[25]);
            cboQFR4.SelectedIndex = 0;

            //semi finals left side
            cboSFL1.Items.Add(teams[0]);
            cboSFL1.Items.Add(teams[8]);
            cboSFL1.SelectedIndex = 0;

            cboSFL2.Items.Add(teams[16]);
            cboSFL2.Items.Add(teams[24]);
            cboSFL2.SelectedIndex = 0;

            //semi finals right side
            cboSFR1.Items.Add(teams[4]);
            cboSFR1.Items.Add(teams[12]);
            cboSFR1.SelectedIndex = 0;

            cboSFR2.Items.Add(teams[20]);
            cboSFR2.Items.Add(teams[28]);
            cboSFR2.SelectedIndex = 0;

            //finals
            cboFinals1.Items.Add(teams[0]);
            cboFinals1.Items.Add(teams[16]);
            cboFinals1.SelectedIndex = 0;

            cboFinals2.Items.Add(teams[4]);
            cboFinals2.Items.Add(teams[20]);
            cboFinals2.SelectedIndex = 0;

            //third place
            txtTP1.Text = teams[16];
            txtTP2.Text = teams[20];

            Finalized();
        }

        #region AppMethods
        private void CBOChanged(object sender, SelectionChangedEventArgs e) //updates colours and teams in the UI to indicate whether a team has won or lost
        {
            List<UIElement> teams = grid.Children.Where(element => element is ComboBox || element is TextBlock).ToList();
            ComboBox cboSender = (ComboBox)sender;


            if (cboSender.Name.Contains("QF"))
                QuarterfinalsCBOChanged(cboSender, teams);
            else if (cboSender.Name.Contains("SF"))
                SemifinalsCBOChanged(cboSender, teams);

            FinalsCBOChanged(cboSender, e);
        }

        public void QuarterfinalsCBOChanged(ComboBox sender, List<UIElement> elements) //updates quarter final teams and any subsequent teams that follow to the chosen team
        {
            SemifinalsCBOChanged(sender, elements);

            foreach (UIElement element in elements)
            {
                if (element is TextBlock)
                {
                    TextBlock txt = (TextBlock)element;

                    if (txt.Text == (string)sender.SelectedItem)
                        txt.Foreground = new SolidColorBrush(Colors.Lime);
                    if (txt.Text != (string)sender.SelectedItem && sender.Items.Any(item => (string)item == txt.Text))
                        txt.Foreground = new SolidColorBrush(Colors.Red);
                }
                else if (element is ComboBox)
                {
                    ComboBox cbo = (ComboBox)element;

                    if (cbo.Name.Contains("SF") && sender.Items.Any(senderItem =>
                                                    cbo.Items.Any(cboItem =>
                                                    senderItem == cboItem)))
                    {
                        var selected = sender.SelectedItem;
                        var unselcted = sender.Items.Where(item => item != selected);
                        cbo.Items.Remove(unselcted.First());
                        cbo.Items.Add(selected);
                        cbo.SelectedIndex = 1;
                    }
                }
                else
                    return;
            }
        }
        public void SemifinalsCBOChanged(ComboBox sender, List<UIElement> elements) //"
        {
            foreach (UIElement element in elements)
            {
                if (element is ComboBox)
                {
                    ComboBox cbo = (ComboBox)element;

                    if (cbo.Name.Contains("Finals") && sender.Items.Any(senderItem => cbo.Items.Any(cboItem => senderItem == cboItem)))
                    {
                        if (cbo.Items == sender.Items)
                            return;

                        var selected = sender.SelectedItem;
                        var unselcted = sender.Items.Where(item => item != selected);
                        cbo.Items.Remove(unselcted.First());
                        cbo.Items.Add(selected);
                        if (cbo.Items.Count == 3)
                            cbo.Items.RemoveAt(2);
                        cbo.SelectedIndex = 1;
                    }
                }
            }
        }

        private void FinalsCBOChanged(object sender, SelectionChangedEventArgs e)//"
        {
            List<ComboBox> teams = grid.Children.Where(element => element is ComboBox).Select(element => (ComboBox)element).ToList();
            List<ComboBox> quarterFinals = teams.Where(team => team.Name.Contains("QF")).ToList();
            List<ComboBox> semiFinals = teams.Where(team => team.Name.Contains("SF")).ToList();
            List<ComboBox> finals = teams.Where(team => team.Name.Contains("Finals")).ToList();

            foreach (ComboBox team in finals)
                team.Foreground = new SolidColorBrush(Colors.Lime);

            foreach (ComboBox semiTeam in semiFinals)
                if (finals.Any(finalTeam => finalTeam.SelectedItem == semiTeam.SelectedItem))
                    semiTeam.Foreground = new SolidColorBrush(Colors.Lime);
                else
                    semiTeam.Foreground = new SolidColorBrush(Colors.Red);

            foreach (ComboBox quarterTeam in quarterFinals)
                if (semiFinals.Any(semiTeam => semiTeam.SelectedItem == quarterTeam.SelectedItem))
                    quarterTeam.Foreground = new SolidColorBrush(Colors.Lime);
                else
                    quarterTeam.Foreground = new SolidColorBrush(Colors.Red);


            var selected1 = cboFinals1.SelectedItem;
            var selected2 = cboFinals2.SelectedItem;
            var unselected1 = cboFinals1.Items.Where(item => item != selected1).ToList();
            var unselected2 = cboFinals2.Items.Where(item => item != selected2).ToList();

            if (unselected1.Count != 0)
            txtTP1.Text = unselected1.First().ToString();
            if (unselected2.Count != 0)
            txtTP2.Text = unselected2.First().ToString();

        }

        private void Finalized() //checks for finalization and loads page accordingly 
        {
            if (MainPage.FileContents.Split("\n").Length == 4 && MainPage.FileContents.Split("\n")[3] == "Finalized")
            {
                var groups = grid.Children.Where(obj => obj is ComboBox).Select(obj => obj as ComboBox).ToList();

                foreach (ComboBox cbo in groups)
                {
                    cbo.IsEnabled = false;
                }

                btnFinalize.IsEnabled = false;
            }
        }
        #endregion

        private void MainMenu_Click(object sender, RoutedEventArgs e) //nagivates to MainMenu page
        {
            Frame.Navigate(typeof(MainPage));
        }

        private void GroupStage_Click(object sender, RoutedEventArgs e) //nagivates to group stage page
        {
            Frame.Navigate(typeof(WCGroupPage));
        }

        private async void BtnFinalize_Click(object sender, RoutedEventArgs e)//saves knockout stage team order and finalizes the save file
        {
            Button btn = (Button)sender;

            if ((string)btn.Content == "Finalize Tournament")
            {
                btn.Content = "Press again to confirm";
                btn.Focus(FocusState.Keyboard);
            }
            else
            {
                string output = $"{fileContents.Split("\n")[0]}\n{fileContents.Split("\n")[1]}\n";

                var textblocks = grid.Children.Where(obj => obj is TextBlock)
                                              .Select(txt => txt as TextBlock)
                                              .Where(txt => txt.Name != "")
                                              .ToList();

                foreach (TextBlock txt in textblocks)
                    output += $"{txt.Text},";

                await FileIO.WriteTextAsync(currentFile, output + "\nFinalized");

                fileContents = output;
                Frame.Navigate(typeof(MainPage));
            }
        }

        private void BtnFinalized_LostFocus(object sender, RoutedEventArgs e) //revets finalize button to normal
        {
            btnFinalize.Content = "Finalize Tournament";
        }
    }
}
