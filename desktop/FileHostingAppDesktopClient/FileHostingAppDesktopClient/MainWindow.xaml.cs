using FileHostingAppDesktopClient.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FileHostingAppDesktopClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private SyncService _syncService;
        private AuthService _authService;
        public string BaseFolder { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Jwt { get; set; }
        public bool SyncRunning { get; set; }
        private DebounceDispatcher debounceTimer = new DebounceDispatcher();
        FileSystemWatcher watcher;
        public MainWindow()
        {
            InitializeComponent();
            _authService = new AuthService();
            SetControlsVisibilityLoggedOut();
            BaseFolder = Settings1.Default.localRootPath;
            buttonBaseFolder.ToolTip = BaseFolder;
            StartWatcher(BaseFolder);
        }

        ~MainWindow()
        {
            if (watcher != null)
            {
                watcher.Dispose();
            }
        }

        private void StartWatcher(string path)
        {
            if (watcher != null)
            {
                watcher.Dispose();
            }
            watcher = new FileSystemWatcher(path);
            watcher.NotifyFilter = NotifyFilters.Attributes
                                | NotifyFilters.CreationTime
                                | NotifyFilters.DirectoryName
                                | NotifyFilters.FileName
                                | NotifyFilters.LastAccess
                                | NotifyFilters.LastWrite
                                | NotifyFilters.Security
                                | NotifyFilters.Size;

            watcher.Changed += Watcher_Changed;
            watcher.Created += Watcher_Changed;
            watcher.Deleted += Watcher_Changed;
            watcher.Renamed += Watcher_Changed;
            watcher.Filter = "*";
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (!SyncRunning)
            {
                if (Jwt != null)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        SyncDebounced();
                    });
                }
            }
        }


        private void HandleSyncEvent(object sender, string e)
        {
            LogMessage(e);
        }

        public void LogMessage(string message)
        {
            logTextBox.Text += DateTime.Now + ": " + message + Environment.NewLine;
            logTextBox.ScrollToEnd();
        }

        private void SyncButton_Click(object sender, RoutedEventArgs e)
        {
            Sync();
        }

        public void SyncDebounced()
        {
            debounceTimer.Debounce(5000, x => Sync());
        }

        public async Task Sync()
        {
            if (SyncRunning) return;
            try
            {
                SyncRunning = true;
                buttonSyncNow.IsEnabled = false;
                await _syncService.SyncAsync();
            }
            catch (Exception e)
            {
                LogMessage("Sync error!");
            }
            finally
            {
                buttonSyncNow.IsEnabled = true;
                SyncRunning = false; 
            }
        }

        private void BaseFolderPickerButtonClick(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    var newPath = dialog.SelectedPath.Replace("\\", "/") + "/";
                    BaseFolder = newPath;
                    buttonBaseFolder.ToolTip = newPath;

                    _syncService = new SyncService(Settings1.Default.cloudAddress, BaseFolder, Jwt);
                    _syncService.MessageEvent += HandleSyncEvent;

                    _syncService.WipeLocalFileHistory();
                    StartWatcher(newPath);
                }
            }
        }

        private void LogoutButtonClick(object sender, RoutedEventArgs e)
        {
            Logout();
        }

        private void LoginButtonClick(object sender, RoutedEventArgs e)
        {
            CredentialsWindow sw = new CredentialsWindow(Email, Password);
            sw.Show();
        }

        public async void Login()
        {
            var loginResult = await _authService.Login(Email, Password);
            if (loginResult.Success)
            {
                Jwt = loginResult.Jwt;
                loginStatusLabel.Content = "Logged in as: " + Email;
                SetControlsVisibilityLoggedIn();
                _syncService = new SyncService(Settings1.Default.cloudAddress, BaseFolder, Jwt);
                _syncService.MessageEvent += HandleSyncEvent;
            }
        }

        public void Logout()
        {
            Email = null;
            Password = null;
            Jwt = null;
            loginStatusLabel.Content = "Not logged in";
            SetControlsVisibilityLoggedOut();
            LogMessage("Logged out!");
        }

        private void SetControlsVisibilityLoggedIn()
        {
            buttonBaseFolder.Visibility = Visibility.Visible;
            buttonSyncNow.Visibility = Visibility.Visible;
            buttonLogout.Visibility = Visibility.Visible;
            buttonLogin.Visibility = Visibility.Collapsed;
        }

        private void SetControlsVisibilityLoggedOut()
        {
            buttonBaseFolder.Visibility = Visibility.Collapsed;
            buttonSyncNow.Visibility = Visibility.Collapsed;
            buttonLogout.Visibility = Visibility.Collapsed;
            buttonLogin.Visibility = Visibility.Visible;
        }

        private void clearLogsButton_Click(object sender, RoutedEventArgs e)
        {
            logTextBox.Text = "";

        }
    }
}
