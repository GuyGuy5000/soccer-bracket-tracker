using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Timers;

namespace FootballAppLib
{
    /// <summary>
    /// Created By: Nadav Hilu
    /// Purpose: custom utility class to retrieve website infromation in various forms.
    /// Last Modified: 2023-06-27
    /// </summary>
    public class WebScraper
    {
        private HttpClient client = new HttpClient();
        private Uri address;
        private string response;
        private List<string> elementList;
        private List<string> targetElementContent;
        private System.Timers.Timer timer = new Timer(2000);

        public string Address
        {
            get { return address.ToString(); }
        }
        public string Response
        {
            get { return response; }
        }
        public List<string> ElementList
        {
            get { return elementList; }
        }
        public List<string> TargetElementContent
        {
            get { return targetElementContent; }
        }


        public WebScraper(Uri uri)
        {
            address = uri;
            elementList = new List<string>();
            targetElementContent = new List<string>();
            client.BaseAddress = address;
            AssignUri();
            timer.Elapsed += SetElementList;
            timer.AutoReset = true;
            timer.Start();
        }

        private async Task AssignUri() //retrieves HTML from address field and assigns it to response property (used only in constructor)s
        {
            Task<HttpResponseMessage> getResponse = client.GetAsync(address.AbsoluteUri);
            getResponse.Wait();
            string getString = await getResponse.Result.Content.ReadAsStringAsync();
            response = getString;
        }

        private void SetElementList(object obj, ElapsedEventArgs e) //seperates HTML elements into an indexed list (used only in constructor)
        {
            if (response == "" || response is null)
                return;

            Regex elementRegex = new Regex("<|>");
            Regex whitespaceRegex = new Regex(@"^\s+$");
            elementList = elementRegex.Split(response).ToList();
            elementList.RemoveAll(s => whitespaceRegex.IsMatch(s) || s == "");

            if (elementList.Count > 0)
                timer.Close();
        }

        public void SetTargetElement(string element) //retrieves all content from a chosen element and stores in property
        {
            targetElementContent.Clear();

            for (int i = elementList.Count - 1; i > 0; i--)
            {
                if (elementList[i].Contains(element))
                    targetElementContent.Add(elementList[i + 1]);
            }
        }

        public static List<string> GetContent(List<string> elementList, string element)
        {
            List<string> content = new List<string>();
            int nestcount = 0;
            string tag = element.Split(" ")[0];

            try
            {
                for (int i = 0; i < elementList.Count - 1; i++)
                {
                    if (Regex.IsMatch(elementList[i], @"^" + element))
                    {
                        i++;

                        for (int j = nestcount; j >= 0; i++)
                        {
                            if (i == elementList.Count - 1)
                                break;
                            if (elementList[i].Split(" ")[0] == "/" + tag)
                            {
                                j--;
                            }
                            else if (elementList[i].Split(" ")[0] == tag)
                                j++;

                            if (j != -1)
                                content.Add(elementList[i]);
                        }
                        nestcount = 0;
                        i--;

                    }
                }
            }
            catch (Exception)
            {
                return new List<string>();
            }

            return content;
        }
    }
}
