using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace LiveTV
{
    public class Channel
    {
        [XmlAttribute("id")]
        public String id { get; set; }

        [XmlElement("display-name")]
        public String name { get; set; }

        [XmlElement("url")]
        public String url { get; set; }

        [XmlElement("icon")]
        public String icon { get; set; }

        public Channel(String id,  String name, String url, String icon)
        {
            this.id = id;
            this.name = name;
            this.url = url;
            this.icon = icon;
        }

        public Channel() { }
    }
}
