using FileHostingAppDesktopClient.Services;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public string BaseFolder { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            _syncService = new SyncService();
            _syncService.MessageEvent += HandleSyncEvent;
        }

        private void HandleSyncEvent(object sender, string e)
        {
            logTextBox.Text += e + Environment.NewLine;
            logTextBox.ScrollToEnd();
        }

        private void SyncButton_Click(object sender, RoutedEventArgs e)
        {
            _syncService.SyncAsync();
        }

        private void SettingsButtonClick(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();                
                ((MainWindow)Application.Current.MainWindow).BaseFolder = dialog.SelectedPath.Replace("\\", "/");
            }

            CredentialsWindow sw = new CredentialsWindow();
            sw.Show();
        }
    }
}
