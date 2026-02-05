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
            DefaultPromptTextBox.Text = Properties.Settings.Default.DefaultPrompt;
            SystemMessageTextBox.Text = Properties.Settings.Default.SystemPrompt;
            OCRPromptTextBox.Text = Properties.Settings.Default.OCRPrompt;
            // set the model text initially (will be overridden by selection if present)
            ModelComboBox.Text = Properties.Settings.Default.Model;
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

            var models = await client.GetModelsAsync(ip);

            // If we got models, populate the ComboBox. Otherwise leave current text (saved model).
            if (models != null && models.Count > 0)
            {
                // preserve user-typed text if not in list
                var current = ModelComboBox.Text;
                ModelComboBox.ItemsSource = models;
                if (!string.IsNullOrEmpty(Properties.Settings.Default.Model))
                {
                    if (models.Contains(Properties.Settings.Default.Model))
                    {
                        ModelComboBox.SelectedItem = Properties.Settings.Default.Model;
                    }
                    else
                    {
                        // keep saved model as editable text even if not in the fetched list
                        ModelComboBox.Text = Properties.Settings.Default.Model;
                    }
                }
                else if (!string.IsNullOrEmpty(current))
                {
                    ModelComboBox.Text = current;
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.DefaultPrompt = DefaultPromptTextBox.Text;
            Properties.Settings.Default.OCRPrompt = OCRPromptTextBox.Text;
            Properties.Settings.Default.SystemPrompt = SystemMessageTextBox.Text;

            // Save model from the ComboBox (editable) — prefer SelectedItem, fall back to Text
            var selectedModel = ModelComboBox.SelectedItem?.ToString();
            Properties.Settings.Default.Model = !string.IsNullOrEmpty(selectedModel) ? selectedModel : ModelComboBox.Text ?? string.Empty;

            Properties.Settings.Default.UseExternalServer = UseExternalServer.IsChecked.Value;
            Properties.Settings.Default.ExternalIP = ExternalIPTextBox.Text;
            Properties.Settings.Default.Save();
            this.Close();
        }
    }
}