using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using TheBitBrine;
using static StereoLog.Toolbox;
using static StereoLog.Emoji;
using System.Reflection;
using System.Diagnostics;

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
        public static string[] EmojiKeys = Emojis.Keys.ToArray();
        public static string[] EmojiVals = Emojis.Values.ToArray();

        public void Run()
        {
            GetUpdates();
            API = new QuickMan();
            var Endpoints = new Dictionary<string, Action<HttpListenerContext>>();

            Endpoints.Add("/", Index2);

            Endpoints.Add("read", Read);


            API.Start(80, Endpoints, 30);
        }

        public void Index(HttpListenerContext Context)
        {
            var HTML = GetPage();
            var Articles = GetArticles();
            var Elements = GetElements();
            var Page = 1;
            if (Context.Request.QueryString.AllKeys.Contains("page"))
                int.TryParse(Context.Request.QueryString["page"], out Page);

            //var ArticleHTML = "";
            //foreach (var Article in Articles.Skip((Page - 1) * 10).Take(10))
            //{

            //    foreach (var Element in GetHashTags(HTML))
            //    {
            //        var iHTMLTemp = Elements.Where(x => x.Name.ToLower().Contains(Element.ToLower()));
            //        if (iHTMLTemp.Any())
            //        {
            //            var iHTML = iHTMLTemp.First().RawHTML;
            //            var DeclaredMembers = Article.GetType().GetFields();
            //            foreach (var Member in DeclaredMembers)
            //            {
            //                if (Member.MemberType == MemberTypes.Field)
            //                {
            //                    var Field = Article.GetType().GetField(Member.Name);
            //                    if (Field != null)
            //                        HTML = HTML.Replace($"##{Element.ToUpper()}##", iHTML.Replace($"##{Member.Name.ToUpper()}##", Field.GetValue(Article).ToString()));
            //                }
            //            }
            //        }

            //    }
            //}

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
            //HTML = HTML.Replace("##ARTICLE.ITEM##", ArticleHTML);



            for (int i = 0; i < Emojis.Count(); i++)
            {
                HTML = HTML.Replace($":{Emojis.Keys.ToList()[i]}:", Emojis.Values.ToList()[i]);
            }

            API.Respond(HTML, "text/html", Context);
        }


        public void Index2(HttpListenerContext Context)
        {
            var sw = new Stopwatch();
            sw.Start();
            var HTML = GetPage();
            var Articles = GetArticles();
            var Elements = GetElements();
            var Page = 1;
            if (Context.Request.QueryString.AllKeys.Contains("page"))
                int.TryParse(Context.Request.QueryString["page"], out Page);

            var ArticleHTML = "";
            foreach (var Article in Articles)
            {
                var property = Article.GetType().GetFields();
                var iHTML = Elements.First(x => x.Name.Contains("article")).RawHTML;
                iHTML = iHTML.Replace("##TITLE##", Article.Title);
                iHTML = iHTML.Replace("##DATE##", Article.Date.ToString("MMM dd yyyy").ToUpper());

                if (Article.Text.Length > 250)
                    Article.Text = $"{Article.Text.Substring(0, 249).TrimEnd()}...".Replace("......", "...");

                var ReadMoreHTML = Elements.First(x => x.Name.ToLower().Contains("readmore")).RawHTML;
                iHTML = iHTML.Replace("##READMORE.ITEM##", ReadMoreHTML);

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
                HTML = HTML.Replace($":{EmojiKeys[i]}:", EmojiVals[i]);
            }

            HTML = HTML.Replace("##STATUS##", $"[{sw.ElapsedMilliseconds:n0} ms]");

            API.Respond(HTML, "text/html", Context);
        }

        public void Read(HttpListenerContext Context)
        {
            var sw = new Stopwatch();
            sw.Start();
            var HTML = GetPage();
            var Articles = GetArticles();
            var Elements = GetElements();

            var ArticleHTML = "";
            foreach (var Article in Articles.Where(x => x.URL.ToLower() == Context.Request.Url.LocalPath.ToLower()))
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
            HTML = HTML.Replace("##READMORE.ITEM##", "");

            for (int i = 0; i < Emojis.Count(); i++)
            {
                HTML = HTML.Replace($":{EmojiKeys[i]}:", EmojiVals[i]);
            }

            HTML = HTML.Replace("##STATUS##", $"[{sw.ElapsedMilliseconds:n0} ms]");
            API.Respond(HTML, "text/html", Context);
        }

        public static string GetPage()
        {
            var HTML = File.ReadAllText("HTML/index.html");
            var CSS = File.ReadAllText("HTML/css/thebitbrine.css");
            CSS = CSS.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace(" ", "");
            return HTML.Replace("##CSS##", CSS);
            //return HTML;
        }

    }
}
