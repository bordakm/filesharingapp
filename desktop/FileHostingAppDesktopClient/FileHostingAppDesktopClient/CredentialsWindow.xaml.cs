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
using System.Windows.Shapes;

namespace FileHostingAppDesktopClient
{
    /// <summary>
    /// Interaction logic for CredentialsWindow.xaml
    /// </summary>
    public partial class CredentialsWindow : Window
    {
        public CredentialsWindow(string currentEmail, string currentPassword)
        {
            InitializeComponent();
            textBoxEmail.Text = currentEmail;
            textBoxPassword.Password = currentPassword;
        }

        private void SaveCredentialsButtonClick(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).Email = textBoxEmail.Text;

            ((MainWindow)Application.Current.MainWindow).Login();

            this.Close();
        }
    }
}
