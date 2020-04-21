using System;
using System.Collections.Generic;
using System.Text;

namespace StereoLog
{
    class Classes
    {
        public class Article
        {
            public string Title;
            public DateTime Date;
            public string Text;
            public string URL;
            public int Page;
        }
        public class Element
        {
            public string Name;
            public string RawHTML;
            public string[] IncludingElements;
        }
    }
}
