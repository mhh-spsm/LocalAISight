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

namespace LocalAISight
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            // If an active profile exists, show its values; otherwise fall back to settings
            var active = ProfilesStore.Instance.ActiveProfile;
            UseExternalServer.IsChecked = Properties.Settings.Default.UseExternalServer;
            ExternalIPTextBox.Text = Properties.Settings.Default.ExternalIP;

            // Load available models (fire-and-forget)
            _ = LoadModelsAsync();

            // refresh models when external server toggle changes or when external IP loses focus
            UseExternalServer.Checked += (_, __) => _ = LoadModelsAsync();
            UseExternalServer.Unchecked += (_, __) => _ = LoadModelsAsync();
            ExternalIPTextBox.LostFocus += (_, __) => _ = LoadModelsAsync();
        }

        private async Task LoadModelsAsync()
        {
            var client = new OllamaClient();
            string ip = null;
            if (UseExternalServer.IsChecked == true && !string.IsNullOrWhiteSpace(ExternalIPTextBox.Text))
            {
                ip = ExternalIPTextBox.Text.Trim();
            }

            // Use the profile's model source if active
            var active = ProfilesStore.Instance.ActiveProfile;
            var models = await client.GetModelsAsync(ip);

        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {


            Properties.Settings.Default.UseExternalServer = UseExternalServer.IsChecked.Value;
            Properties.Settings.Default.ExternalIP = ExternalIPTextBox.Text;

            // If a profile is active, update it; otherwise update settings
            var activeProfile = ProfilesStore.Instance.ActiveProfile;
            this.Close();
        }
    }
}