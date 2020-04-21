using Markdig;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static StereoLog.Classes;

namespace StereoLog
{
    class Toolbox
    {
        public static Article[] GetArticles()
        {
            var Path = "Articles";
            var ArticleList = new List<Article>();
            try
            {
                if (Directory.Exists(Path))
                {
                    var Files = Directory.GetFiles(Path, "*.md");
                    for (int i = 0; i < Files.Length; i++)
                    {
                        try
                        {
                            var iArticle = new Article();
                            iArticle.Title = Files[i].Split(new char[] { '/', '\\' }).Last().Replace(".md", "");
                            iArticle.Text = Markdig.Markdown.ToHtml(System.IO.File.ReadAllText(Files[i]));
                            iArticle.Date = System.IO.File.GetCreationTime(Path);
                            iArticle.URL = $"/{iArticle.Date.ToString("dd-MMMM-yyyy")}-{RemoveSpecialChars(iArticle.Title.Replace(" ", "-"))}";
                            iArticle.Page = 10 / i + 1;
                            ArticleList.Add(iArticle);
                        }
                        catch { }
                    }
                }
            }
            catch { }
            return ArticleList.ToArray();
        }

        public static Element[] GetElements()
        {
            var Path = "HTML/elements";
            var ElementList = new List<Element>();
            try
            {
                if (Directory.Exists(Path))
                {
                    var Files = Directory.GetFiles(Path, "*.sle");
                    foreach (var File in Files)
                    {
                        try
                        {
                            var iElement = new Element();
                            iElement.Name = File.Split(new char[] { '/', '\\' }).Last().Replace(".sle", "");
                            iElement.RawHTML = System.IO.File.ReadAllText(File);
                            iElement.IncludingElements = GetHashTags(iElement.RawHTML);
                            ElementList.Add(iElement);
                        }
                        catch { }
                    }
                }
            }
            catch { }
            return ElementList.ToArray();
        }

        public static string[] GetHashTags(string RawHTML)
        {
            var HashTags = new List<string>();
            var Temp = "";
            var HashReached = false;
            for (int i = 0; i < RawHTML.Length; i++)
            {
                if (RawHTML[i] == '#' && i + 1 < RawHTML.Length && RawHTML[i + 1] == '#')
                {
                    if (HashReached)
                    {
                        HashTags.Add(Temp);
                        HashReached = false;
                        Temp = "";
                    }
                    else
                        HashReached = true;

                }
                else
                {
                    if(HashReached && RawHTML[i] != '#')
                        Temp += RawHTML[i];
                }
            }
            return HashTags.ToArray();
        }

        public static string RemoveSpecialChars(string str)
        {
            string CleanText = "";
            for (int i = 0; i < str.Length; i++)
            {
                if (char.IsLetterOrDigit(str[i]) || str[i] == '-' || str[i] == '_')
                    CleanText += str[i];
            }
            return CleanText;
        }
    }
}
