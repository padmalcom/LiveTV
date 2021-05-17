using System;
using System.Collections.Generic;
using System.Text;

namespace LiveTV
{
    class Channel
    {
        public String name { get; set; }
        public String url { get; set; }

        public String icon { get; set; }

        public Channel(String name, String url, String icon)
        {
            this.name = name;
            this.url = url;
            this.icon = icon;
        }
    }
}
