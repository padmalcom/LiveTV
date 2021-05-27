using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using LibVLCSharp.Shared;
using M3U.NET;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;
using YamlDotNet.Serialization;

namespace LiveTV
{
    public partial class Form1 : Form
    {
        private LibVLC _libVLC;
        private MediaPlayer _mp;

        private List<Channel> channels = new List<Channel>();
        private List<Button> channelButtons = new List<Button>();

        private bool fullscreen = false;

        private const int FULLSCREEN_COOLDOWN_MAX = 100;
        private const int MARQUEE_COOLDOWN_MAX = 4000;

        private int fullscreenCooldown = FULLSCREEN_COOLDOWN_MAX;

        private Programme currentProgramme = null;
        private string currentChannel = "";

        private Point oldLocation = new Point();
        private Size oldSize = new Size();

        // App properties
        Settings settings = null;
        Tv tvProgram = null;

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

        private void readProperties()
        {
            // Create file path
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string liveTvAppFolder = Path.Combine(appDataFolder, "livetv");
            string liveTvProperties = Path.Combine(liveTvAppFolder, "properties.yml");

            // Read config
            if (!File.Exists(liveTvProperties))
            {
                Directory.CreateDirectory(liveTvAppFolder);
                File.Create(liveTvProperties).Close();
            }

            using (var reader = new StreamReader(liveTvProperties))
            {
                string yamlStr = File.ReadAllText(liveTvProperties, Encoding.UTF8);
                var deserializer = new DeserializerBuilder().Build();
                settings = deserializer.Deserialize<Settings>(yamlStr);
                if (settings == null)
                {
                    settings = new Settings();
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

            timer1.Interval = settings.timeoutFullscreenButton;

            if (File.Exists(playlistFile))
            {
                M3UFile m3u = new M3UFile(new FileInfo(playlistFile));
                channels.Clear();

                foreach (MediaItem me in m3u.Files)
                {
                    // Is the channel blacklisted?
                    if (settings.channelBlacklist.Contains(me.Inf))
                    {
                        continue;
                    }

                    string execDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                    string iconPath = "";
                    if (File.Exists(Path.Combine(execDir, "channelimg", me.Inf + ".png")))
                    {
                        iconPath = Path.Combine(execDir, "channelimg", me.Inf + ".png");
                    }
                    channels.Add(new Channel(me.Inf, me.Inf, me.Location, iconPath));
                }
            }
            else
            {
                MessageBox.Show("Could not find or download channel playlist. Closing.");
                Application.Exit();
            }

            // Create icons
            createChannelItems();

            // Update program
            getEPG();
        }

        private void onChannelSelect(object sender, EventArgs e)
        {
            Button b = sender as Button;
            var media = new LibVLCSharp.Shared.Media(_libVLC, new Uri(((b.Tag as Channel).url)));
            _mp.Play(media);
            currentChannel = (b.Tag as Channel).name;
            Programme cp = getcurrentProgramme((b.Tag as Channel).name);
            if (cp != null)
            {
                // Documentation: https://github.com/videolan/libvlcsharp/blob/3.x/docs/how_do_I_do_X.md#how-do-i-use-marquee-
                videoView1.MediaPlayer.SetMarqueeInt(VideoMarqueeOption.Enable, 1);
                videoView1.MediaPlayer.SetMarqueeInt(VideoMarqueeOption.Timeout, MARQUEE_COOLDOWN_MAX);
                videoView1.MediaPlayer.SetMarqueeInt(VideoMarqueeOption.Position, 8);
                videoView1.MediaPlayer.SetMarqueeString(VideoMarqueeOption.Text, cp.channel + ": "+ cp.title + " " + cp.start.ToShortTimeString() + " - " + cp.stop.ToShortTimeString());
                this.Text = "liveTV " + cp.channelUnlocalized() + ": " + cp.title;
            } else
            {
                this.Text = "liveTV";
            }
    
        }

        private void record()
        {
            //https://github.com/mfkl/libvlcsharp-samples/blob/master/ScreenRecorder/Program.cs
        }

        private void onChannelRemove(object sender, EventArgs e)
        {
            MenuItem mi = sender as MenuItem;
            if (!settings.channelBlacklist.Contains((String)mi.Tag))
            {
                settings.channelBlacklist.Add((String)mi.Tag.ToString());
            }
            ContextMenu p1 = mi.Parent as ContextMenu;
            p1.SourceControl.Dispose();
        }

        private void createChannelItems()
        {
            foreach (Button b in channelButtons)
            {
                b.Dispose();
            }

            int bWidth = settings.channelButtonSize;
            int bHeight = settings.channelButtonSize;

            for (int i = 0; i < channels.Count; i++)
            {
                Button b = new Button();

                b.Width = bWidth;
                b.Height = bHeight;

                if (!channels[i].icon.Trim().Equals(""))
                {
                    b.BackgroundImage = Image.FromFile(channels[i].icon);
                    b.BackgroundImageLayout = ImageLayout.Zoom;
                }
                else
                {
                    b.Text = channels[i].name;
                }
                b.Tag = channels[i];

                b.ContextMenu = new ContextMenu();
                MenuItem mi = new MenuItem("Remove " + channels[i].name, onChannelRemove);
                mi.Tag = channels[i].name;
                b.ContextMenu.MenuItems.Add(mi);
                b.Click += new EventHandler(onChannelSelect);
                flowLayoutPanel1.Controls.Add(b);
            }
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            readProperties();
            Color col = button1.BackColor;
            button1.BackColor = Color.FromArgb(50, col.R, col.G, col.B);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (fullscreen)
            {
                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.Size = this.oldSize;
                this.Location = oldLocation;
                panel1.Height = this.Height - flowLayoutPanel1.Height- 60;
            }
            else
            {
                this.oldSize = new Size(this.Size.Width, this.Size.Height);
                this.oldLocation = new Point(this.Location.X, this.Location.Y);

                this.FormBorderStyle = FormBorderStyle.None;
                var rectangle = Screen.FromControl(this).Bounds;
                this.Size = new Size(rectangle.Width, rectangle.Height);
                this.Location = new Point(0, 0);

                panel1.Height = this.Height;
            }

            fullscreen = !fullscreen;
        }



        private void timer1_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (fullscreenCooldown > 0)
            {
                fullscreenCooldown -= 1;
            } else
            {
                button1.Hide();
            }
        }


        private void videoView1_MouseMove(object sender, MouseEventArgs e)
        {
            if (button1.Bounds.Contains(e.Location))
            {
                fullscreenCooldown = FULLSCREEN_COOLDOWN_MAX;
                if (!button1.Visible)
                {
                    button1.Show();
                }
            }
        }

        private void button1_MouseMove(object sender, MouseEventArgs e)
        {
            fullscreenCooldown = FULLSCREEN_COOLDOWN_MAX;
            button1.Show();
        }

        private Programme getcurrentProgramme(string channel)
        {
            if (tvProgram != null)
            {
                List<Programme> channelPrograms = tvProgram.programme.FindAll(e =>
                    (e.channelUnlocalized().Equals(channel, StringComparison.OrdinalIgnoreCase)) &&
                    ((e.start.Date == DateTime.Today) || (e.stop.Date == DateTime.Today)) &&
                    (DateTime.Now > e.start) && (DateTime.Now < e.stop));

                if (channelPrograms.Count > 0)
                {
                    currentProgramme = channelPrograms[0];
                    return channelPrograms[0];
                }
            }
            return null;
        }

        private void getEPG()
        {
            
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string liveTvAppFolder = Path.Combine(appDataFolder, "livetv");
            string epgZipFile = Path.Combine(liveTvAppFolder, "epg.gz");
            if (File.Exists(epgZipFile))
            {
                if (File.GetCreationTime(epgZipFile) == DateTime.Today)
                {
                    updateEpg(epgZipFile);
                    return;
                } else
                {
                    File.Delete(epgZipFile); 
                }
            }

            WebClient wc = new WebClient();
            wc.DownloadFileCompleted += (sender, e) => updateEpg(epgZipFile);
            wc.DownloadFileAsync(new Uri(settings.epgDownload), epgZipFile);
        }

        private void updateEpg(String archive)
        {
            string EPG_XML_FILE = "epg.xml";
            String EPG_XML_PATH_ABSOLUTE = Path.Combine(Path.GetDirectoryName(archive), EPG_XML_FILE);

            // Delete old epg
            if (File.Exists(EPG_XML_PATH_ABSOLUTE))
            {
                File.Delete(EPG_XML_PATH_ABSOLUTE);
            }

            ExtractEPGGZip(archive, EPG_XML_PATH_ABSOLUTE);

            if (File.Exists(EPG_XML_PATH_ABSOLUTE))
            {
                XmlSerializer xmls = new XmlSerializer(typeof(Tv));
                using (Stream reader = new FileStream(EPG_XML_PATH_ABSOLUTE, FileMode.Open))
                {
                    tvProgram = (Tv)xmls.Deserialize(reader);
                }
                if (tvProgram != null)
                {
                    label1.Text = "Programm geladen";
                }
            } else
            {
                MessageBox.Show("EPG extraction failed.");
            }
            flowLayoutPanel1.Enabled = true;
        }

        public void ExtractEPGGZip(string gzipFileName, string targetFile)
        {
            // Use a 4K buffer. Any larger is a waste.    
            byte[] dataBuffer = new byte[4096];

            using (System.IO.Stream fs = new FileStream(gzipFileName, FileMode.Open, FileAccess.Read))
            {
                using (GZipInputStream gzipStream = new GZipInputStream(fs))
                {
                    using (FileStream fsOut = File.Create(targetFile))
                    {
                        StreamUtils.Copy(gzipStream, fsOut, dataBuffer);
                    }
                }
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            var serializer = new SerializerBuilder().Build();
            var yaml = serializer.Serialize(settings);
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string liveTvAppFolder = Path.Combine(appDataFolder, "livetv");
            string liveTvProperties = Path.Combine(liveTvAppFolder, "properties.yml");
            File.WriteAllText(liveTvProperties, yaml);
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            // Check Program update

            if (tvProgram != null)
            {
                Programme cp = getcurrentProgramme(currentChannel);
                if (cp != null)
                {
                    if (!cp.title.Equals(currentProgramme.title))
                    {
                        currentProgramme = cp;
                        videoView1.MediaPlayer.SetMarqueeInt(VideoMarqueeOption.Enable, 1);
                        videoView1.MediaPlayer.SetMarqueeInt(VideoMarqueeOption.Timeout, MARQUEE_COOLDOWN_MAX);
                        videoView1.MediaPlayer.SetMarqueeInt(VideoMarqueeOption.Position, 8);
                        videoView1.MediaPlayer.SetMarqueeString(VideoMarqueeOption.Text, cp.channelUnlocalized() + ": " + cp.title + " " + cp.start.ToShortTimeString() + " - " + cp.stop.ToShortTimeString());
                        this.Text = "liveTV " + cp.channelUnlocalized() + ": " + cp.title;
                    }
                }
            } else
            {
                this.Text = "liveTV"; ;
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.stonedrum.de");
        }
    }
}
