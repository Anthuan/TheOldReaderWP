using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PhoneApp1
{
    public partial class SettingsPage : PhoneApplicationPage
    {
        public SettingsPage()
        {
            InitializeComponent();
            if (App.Current.TheOldReaderManager != null)
            {
                if (!App.Current.TheOldReaderManager.IsAuthenticated && !App.Current.TheOldReaderManager.AuthenticationInProgress)
                {
                    Override.IsEnabled = false;
                    Override.IsChecked = false;
                    AlreadyAuthenticated.IsEnabled = false;
                    AlreadyAuthenticated.IsChecked = false;
                    Username.IsEnabled = true;
                    Password.IsEnabled = true;
                    ButtonCheckCredentials.IsEnabled = true;
                }
            }
            else
            {
                Override.Visibility = System.Windows.Visibility.Collapsed;
                Override.IsChecked = false;
                Override.IsEnabled = true;
                AlreadyAuthenticated.IsChecked = false;
                AlreadyAuthenticated.IsEnabled = false;
                AlreadyAuthenticated.Visibility = System.Windows.Visibility.Collapsed;
                Username.IsEnabled = true;
                Password.IsEnabled = true;
                ButtonCheckCredentials.IsEnabled = true;
            }
            // DEBUGGING ONLY!
            //Username.Text = "hardcodeduser";
            //Password.Password = "hardcodedpassword";
        }

        private async void StopBar()
        {
            ProgressBarObject.IsIndeterminate = false;
            ProgressBarObject.Visibility = System.Windows.Visibility.Collapsed;
        }

        private async void StartBar()
        {
            ProgressBarObject.IsIndeterminate = true;
            ProgressBarObject.Visibility = System.Windows.Visibility.Visible;
        }

        private void Override_Checked(object sender, RoutedEventArgs e)
        {
            Username.IsEnabled = true;
            Password.IsEnabled = true;
            ButtonCheckCredentials.IsEnabled = true;
        }

        private void Override_Unchecked(object sender, RoutedEventArgs e)
        {
            Username.IsEnabled = false;
            Password.IsEnabled = false;
            ButtonCheckCredentials.IsEnabled = false;
        }

        private delegate void AuthenticateHandler(string username, string password);

        private async void ButtonCheckCredentials_Click(object sender, RoutedEventArgs e)
        {
            StartBar();
            Dispatcher.BeginInvoke(new AuthenticateHandler(TheOldReader.StaticAuthenticate), Username.Text, Password.Password);
            while (!await AnthuanUtils.FileExists("token"))
            {
                System.Threading.Thread.Sleep(1000);
            }
            NavigationService.GoBack();
        }

        private void ReAuthenticationCompleted(object sender, EventArgs e)
        {
            
        }
    }
}