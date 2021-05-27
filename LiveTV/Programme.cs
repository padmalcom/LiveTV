using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace LiveTV
{
    public class Programme
    {
        [XmlIgnore]
        public DateTime start { get; set; }

        [XmlIgnore]
        public DateTime stop { get; set; }
        
        [XmlAttribute("start")]
        public string startString {
            get {
                return this.start.ToString("dd.MM.yyyy HH:mm:ss");
            }
            set {
                this.start = DateTime.ParseExact(value, "yyyyMMddHHmmss zzz", CultureInfo.InvariantCulture);
            }
        }

        [XmlAttribute("stop")]
        public string stopString {
            get
            {
                return this.stop.ToString("dd.MM.yyyy HH:mm:ss");
            }
            set
            {
                this.stop = DateTime.ParseExact(value, "yyyyMMddHHmmss zzz", CultureInfo.InvariantCulture);
            }
        }

        [XmlAttribute("channel")]
        public String channel
        {
            get;
            set;
        }

        [XmlElement("title")]
        public String title { get; set; }

        [XmlElement("desc")]
        public String description { get; set; }

        [XmlIgnore]
        public DateTime date { get; set; }

        [XmlElement("date")]
        public string dateString { 
            get
            {
                return this.date.ToString("dd.MM.yyyy HH:mm:ss");
            }
            set
            {
                this.date = DateTime.ParseExact(value, "yyyy", CultureInfo.InvariantCulture);
            }
        }

        [XmlElement("category")]
        public List<String> categories { get; set; }

        [XmlElement("country")]
        public String country { get; set; }

        public string channelUnlocalized()
        {
            if (this.channel.EndsWith("de", StringComparison.CurrentCultureIgnoreCase))
            {
                return this.channel.Substring(0, this.channel.Length - 2);
            }
            return this.channel;
        }
    }
}
