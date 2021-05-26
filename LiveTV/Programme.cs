using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace LiveTV
{
    class Programme
    {
        [XmlAttribute("start", DataType = "date")]
        public DateTime start{get; set;}

        [XmlAttribute("stop", DataType = "date")]
        public DateTime stop { get; set; }

        [XmlAttribute("channel")]
        public String channel { get; set; }

        [XmlElement("title")]
        public String title { get; set; }

        [XmlElement("desc")]
        public String description { get; set; }

        [XmlElement("date", DataType = "date")]
        public DateTime date { get; set; }

        [XmlElement("category")]
        public List<String> categories { get; set; }

        [XmlElement("country")]
        public String country { get; set; }
    }
}
