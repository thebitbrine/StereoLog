using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using TheBitBrine;
using static StereoLog.Toolbox;
using static StereoLog.Emoji;
namespace StereoLog
{
    class Program
    {
        static void Main(string[] args)
        {
            new Program().Run();
        }

        public static QuickMan API;
        public static Dictionary<string, string> Emojis = GetEmojis();
        public void Run()
        {
            API = new QuickMan();
            var Endpoints = new Dictionary<string, Action<HttpListenerContext>>();

            Endpoints.Add("/", Index);

            Endpoints.Add("read", Read);


            API.Start(80, Endpoints, 30);
        }

        public void Index(HttpListenerContext Context)
        {
            var HTML = File.ReadAllText("HTML/index.html");
            var Articles = GetArticles();
            var Elements = GetElements();
            var Page = 1;
            if (Context.Request.QueryString.AllKeys.Contains("page"))
                int.TryParse(Context.Request.QueryString["page"], out Page);

            var ArticleHTML = "";
            foreach (var Article in Articles.Skip((Page - 1) * 10).Take(10))
            {
                var iHTML = Elements.First(x => x.Name.Contains("article")).RawHTML;
                iHTML = iHTML.Replace("##TITLE##", Article.Title);
                iHTML = iHTML.Replace("##DATE##", Article.Date.ToString("MMM dd yyyy").ToUpper());
                iHTML = iHTML.Replace("##TEXT##", Article.Text);
                iHTML = iHTML.Replace("##URL##", Article.URL);
                ArticleHTML += iHTML;
            }

            var SideBarHTML = "";
            foreach (var Article in Articles)
            {
                var iHTML = Elements.First(x => x.Name.Contains("sidebar")).RawHTML;
                var SmallTitle = Article.Title;
                if (SmallTitle.Length > 19)
                    SmallTitle = $"{SmallTitle.Substring(0, 20).TrimEnd()}...".Replace("......", "...");

                iHTML = iHTML.Replace("##TITLE##", SmallTitle);
                iHTML = iHTML.Replace("##URL##", Article.URL);
                SideBarHTML += iHTML;
            }

            HTML = HTML.Replace("##SIDEBAR.ITEM##", SideBarHTML);
            HTML = HTML.Replace("##ARTICLE.ITEM##", ArticleHTML);

            for (int i = 0; i < Emojis.Count(); i++)
            {
                HTML = HTML.Replace($":{Emojis.Keys.ToList()[i]}:", Emojis.Values.ToList()[i]);
            }

            API.Respond(HTML, "text/html", Context);
        }

        public void Read(HttpListenerContext Context)
        {

        }

    }
}
