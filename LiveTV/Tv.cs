using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace LiveTV
{
    [Serializable()]
    [XmlRoot(Namespace = "", ElementName = "tv", DataType = "string", IsNullable = true)]
    public class Tv
    {
        [XmlAttribute("generator-info-name")]
        public String generatorInfoName { get; set; }

        [XmlAttribute("generator-info-url")]
        public String generatorInfoUrl { get; set; }

        [XmlElement("channel")]
        public List<Channel> channels { get; set; }

        [XmlElement("programme")]
        public List<Programme> programme { get; set; }
    }
}
