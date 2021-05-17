using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibVLCSharp.Shared;
using System.IO;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using System.Net;
using M3U.NET;
namespace LiveTV
{
    public partial class Form1 : Form
    {
        private LibVLC _libVLC;
        private MediaPlayer _mp;

        private List<Channel> channels = new List<Channel>();
        private List<Button> channelButtons = new List<Button>();

        public Form1()
        {
            if (!DesignMode)
            {
                Core.Initialize();
            }

            InitializeComponent();
            _libVLC = new LibVLC();
            _mp = new MediaPlayer(_libVLC);         
            videoView1.MediaPlayer = _mp;
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                //
            }
        }

        private void readProperties()
        {
            string language = null;

            // Create file path
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string liveTvAppFolder = Path.Combine(appDataFolder, "livetv");
            string liveTvProperties = Path.Combine(liveTvAppFolder, "properties.yml");

            // Read config
            if (!File.Exists(liveTvProperties))
            {
                Directory.CreateDirectory(liveTvAppFolder);
                File.Create(liveTvProperties);
            }

            using (var reader = new StreamReader(liveTvProperties))
            {
                string yamlStr = File.ReadAllText(liveTvProperties, Encoding.UTF8);
                var deserializer = new DeserializerBuilder().Build();
                var res = deserializer.Deserialize<dynamic>(yamlStr);
                if (res != null)
                {
                    if (res["app"] != null)
                    {
                        language = res["app"]["language"];
                    }
                }
            }

            // Read channels
            string playlistFile = Path.Combine(liveTvAppFolder, "tvsd.m3u");
            if (!File.Exists(playlistFile))
            {
                WebClient wc = new WebClient();
                wc.DownloadFile("http://192.168.178.1/dvb/m3u/tvsd.m3u", playlistFile);
                MessageBox.Show("Downloaded tvsd.m3u playlist successfully.");
            }

            if (File.Exists(playlistFile))
            {
                M3UFile m3u = new M3UFile(new FileInfo(playlistFile));
                channels.Clear();

                foreach (MediaItem me in m3u.Files)
                {
                    string execDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                    string iconPath = "";
                    if (File.Exists(Path.Combine(execDir, me.Inf + ".png")))
                    {
                        iconPath = Path.Combine(execDir, me.Inf + ".png");
                    }
                    channels.Add(new Channel(me.Inf, me.Location, iconPath));
                }
            } else
            {
                MessageBox.Show("Could not find or download channel playlist. Closing.");
                Application.Exit();
            }

            // Create icons
            createChannelItems();

        }

        private void createChannelItems()
        {
            foreach(Button b in channelButtons)
            {
                b.Dispose();
            }

            int bWidth = this.Width / 8;
            int bHeight = bWidth;

            for (int i=0; i<channels.Count; i++)
            {
                Button b = new Button();
                
                b.Left = (i % 8) * bWidth;
                b.Top = (i / 8) * bHeight;
                b.Width = bWidth;
                b.Height = bHeight;
                if (!channels[i].icon.Trim().Equals(""))
                {
                    b.BackgroundImage = Image.FromFile(channels[i].icon);
                    b.BackgroundImageLayout = ImageLayout.Stretch;
                } else
                {
                    b.Text = channels[i].name;
                }
                b.Tag = channels[i].url;
                b.Click += new EventHandler(onChannelSelect);
                flowLayoutPanel1.Controls.Add(b);
            }
        }

        private void onChannelSelect(object sender, EventArgs e)
        {
            Button b = sender as Button;
            var media = new LibVLCSharp.Shared.Media(_libVLC, new Uri(b.Tag as String));
            _mp.Play(media);
        }

        private void updateChannels()
        {
            //http://192.168.178.1/dvb/m3u/tvsd.m3u
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            readProperties();
            //var media = new LibVLCSharp.Shared.Media(_libVLC, new Uri("http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4"));
            //_mp.Play(media);
        }
    }
}
