using System.Windows;
using LocalAISight.Models;

namespace LocalAISight
{
    public partial class ProfileEditorWindow : Window
    {
        public Profile Profile { get; private set; }

        public ProfileEditorWindow()
        {
            InitializeComponent();
            Profile = new Profile();
            // Load available models into the ComboBox
            _ = LoadModelsAsync();
        }

        public ProfileEditorWindow(Profile existing) : this()
        {
            Profile = existing;
            NameBox.Text = existing.Name;
            SystemBox.Text = existing.SystemPrompt;
            DefaultBox.Text = existing.DefaultPrompt;
            OCRPromptTextBox.Text = existing.OCRPrompt;
            ModelComboBox.Text = existing.Model;
        }

        private async System.Threading.Tasks.Task LoadModelsAsync()
        {
            var client = new OllamaClient();
            var models = await client.GetModelsAsync();
                ModelComboBox.ItemsSource = models;
            if (!string.IsNullOrEmpty(Profile.Model) && models.Contains(Profile.Model))
                ModelComboBox.SelectedItem = Profile.Model;
            else
                ModelComboBox.Text = Profile.Model;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Profile.Name = NameBox.Text;
                Profile.Model = ModelComboBox.SelectedItem?.ToString() ?? ModelComboBox.Text ?? string.Empty;
            Profile.SystemPrompt = SystemBox.Text;
            Profile.DefaultPrompt = DefaultBox.Text;
                Profile.OCRPrompt = OCRPromptTextBox.Text;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}