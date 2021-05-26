using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveTV
{
    class Settings
    {
        public string language = "DE";
        public int channelButtonSize = 40;
        public int timeoutFullscreenButton = 1;
        public List<String> channelBlacklist = new List<String>();
        public string epgDownload = "https://bit.ly/FreeEPG";
    }
}
