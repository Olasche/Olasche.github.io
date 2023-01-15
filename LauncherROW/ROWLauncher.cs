using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Common;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Forms;

namespace LauncherROW
{
    enum LauncherStatus
    {
        ready,
        failed,
        downloadingGame,
        downloadingUpdate
    }

    public partial class ROWLauncher : Form
    {
        private static readonly string version_file_link = "https://drive.google.com/uc?export=download&id=1Pst2I1aOIeB8232pYD6nV7u2uE72mqVf";
        private static readonly string game_rar_link = "https://drive.google.com/uc?export=download&id=12KKDjbVDjMM-5e6fL1OfeEoLouxbzgnb";
        private static readonly string zip_extension = ".rar";
        private static readonly string builds_folder_name = "Builds";
        private static readonly string executable_name = "ROW";

        private string rootPath;
        private string versionFile;
        private string gameZip;
        private string gameExe;

        private LauncherStatus _status;
        internal LauncherStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                switch (_status)
                {
                    case LauncherStatus.ready:
                        PlayButton.Text = "Play";
                        break;
                    case LauncherStatus.failed:
                        PlayButton.Text = "Update Failed - Retry";
                        break;
                    case LauncherStatus.downloadingGame:
                        PlayButton.Text = "Downloading Game";
                        break;
                    case LauncherStatus.downloadingUpdate:
                        PlayButton.Text = "Downloading Update";
                        break;
                    default:
                        break;
                }
            }
        }
        public ROWLauncher()
        {
            InitializeComponent();
            rootPath = Directory.GetCurrentDirectory();
            versionFile = Path.Combine(rootPath, "Version.txt");
            gameZip = Path.Combine(rootPath, builds_folder_name + zip_extension);
            gameExe = Path.Combine(rootPath, builds_folder_name, executable_name + ".exe");
        }

        private void CheckForUpdates()
        {
            PlayButton.Enabled = false;
            if (File.Exists(versionFile))
            {
                Version localVersion = new Version(File.ReadAllText(versionFile));
                VersionText.Text = localVersion.ToString();

                try
                {
                    WebClient webClient = new WebClient();
                    Version onlineVersion = new Version(webClient.DownloadString(version_file_link));

                    if (onlineVersion.IsDifferentThan(localVersion))
                    {
                        InstallGameFiles(true, onlineVersion);
                    }
                    else
                    {
                        Status = LauncherStatus.ready;
                        PlayButton.Enabled = true;
                    }
                }
                catch (Exception ex)
                {
                    Status = LauncherStatus.failed;
                    PlayButton.Enabled = true;
                    MessageBox.Show($"Error checking for game updates: {ex}");
                }
            }
            else
            {
                InstallGameFiles(false, Version.zero);
            }
        }

        private void InstallGameFiles(bool _isUpdate, Version _onlineVersion)
        {
            try
            {
                WebClient webClient = new WebClient();
                if (_isUpdate)
                {
                    Status = LauncherStatus.downloadingUpdate;
                }
                else
                {
                    Status = LauncherStatus.downloadingGame;
                    _onlineVersion = new Version(webClient.DownloadString(version_file_link));
                }
                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadGameCompletedCallback);
                webClient.DownloadFileAsync(new Uri(game_rar_link), gameZip, _onlineVersion);
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                PlayButton.Enabled = true;
                MessageBox.Show($"Error installing game files: {ex}");
            }
        }

        private void DownloadGameCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                string onlineVersion = ((Version)e.UserState).ToString();

                using (var archive = RarArchive.Open(gameZip))
                {
                    foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                    {
                        entry.WriteToDirectory(rootPath, new ExtractionOptions()
                        {
                            ExtractFullPath = true,
                            Overwrite = true
                        });
                    }
                }

                File.Delete(gameZip);

                File.WriteAllText(versionFile, onlineVersion);

                VersionText.Text = onlineVersion;
                Status = LauncherStatus.ready;
                PlayButton.Enabled = true;
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                PlayButton.Enabled = true;
                MessageBox.Show($"Error finishing download: {ex}");
            }
        }
        private void ROWLauncher_Load(object sender, EventArgs e)
        {
            CloseButton.BackColor = ColorTranslator.FromHtml("#FF990033");
            CloseButton.ForeColor = ColorTranslator.FromHtml("#FFFFB066");
            CloseButton.FlatAppearance.BorderColor = ColorTranslator.FromHtml("#FFFFB066");
            CloseButton.FlatAppearance.BorderSize = 2;
            MinimizeButton.BackColor = ColorTranslator.FromHtml("#FF990033");
            MinimizeButton.ForeColor = ColorTranslator.FromHtml("#FFFFB066");
            MinimizeButton.FlatAppearance.BorderColor = ColorTranslator.FromHtml("#FFFFB066");
            MinimizeButton.FlatAppearance.BorderSize = 2;
            PlayButton.BackColor = ColorTranslator.FromHtml("#FF990033");
            PlayButton.ForeColor = ColorTranslator.FromHtml("#FFFFB066");
            PlayButton.FlatAppearance.BorderColor = ColorTranslator.FromHtml("#FFFFB066");
            PlayButton.FlatAppearance.BorderSize = 2;
            CheckForUpdates();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics, ClientRectangle, ColorTranslator.FromHtml("#FFFFB066"), ButtonBorderStyle.Solid);
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void MinimizeButton_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
        }

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        private void ROWLauncher_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void background_MouseDown(object sender, MouseEventArgs e)
        {
            ROWLauncher_MouseDown(sender, e);
        }

        private void PlayButton_Click(object sender, EventArgs e)
        {
            if (File.Exists(gameExe) && Status == LauncherStatus.ready)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(gameExe);
                startInfo.WorkingDirectory = Path.Combine(rootPath, builds_folder_name);
                Process.Start(startInfo);

                Close();
            }
            else if (Status == LauncherStatus.failed)
            {
                CheckForUpdates();
            }
        }
    }

    struct Version
    {
        internal static Version zero = new Version(0, 0, 0);

        private short major;
        private short minor;
        private short subMinor;

        internal Version(short _major, short _minor, short _subMinor)
        {
            major = _major;
            minor = _major;
            subMinor = _subMinor;
        }
        internal Version(string _version)
        {
            string[] _versionString = _version.Split('.');
            if (_versionString.Length != 3)
            {
                major = 0;
                minor = 0;
                subMinor = 0;
                return;
            }
            major = short.Parse(_versionString[0]);
            minor = short.Parse(_versionString[1]);
            subMinor = short.Parse(_versionString[2]);
        }

        internal bool IsDifferentThan(Version _otherVersion)
        {
            if (major != _otherVersion.major)
                return true;
            else if (minor != _otherVersion.minor)
                return true;
            else if (subMinor != _otherVersion.subMinor)
                return true;
            return false;
        }

        public override string ToString()
        {
            return $"{major}.{minor}.{subMinor}";
        }
    }
}
