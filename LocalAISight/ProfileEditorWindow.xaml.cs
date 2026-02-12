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
            // ModelBox may be a ComboBox; set its text or selected item accordingly
            if (this.FindName("ModelBox") is System.Windows.Controls.ComboBox cb)
            {
                cb.Text = existing.Model;
            }
            else
            {
                try { ModelBox.Text = existing.Model; } catch { }
            }
            SystemBox.Text = existing.SystemPrompt;
            DefaultBox.Text = existing.DefaultPrompt;
            // OCRBox may be null in older files; look it up dynamically
            var oc = this.FindName("OCRBox") as System.Windows.Controls.TextBox;
            if (oc != null)
            {
                oc.Text = existing.OCRPrompt;
            }
        }

        private async System.Threading.Tasks.Task LoadModelsAsync()
        {
            var client = new OllamaClient();
            var models = await client.GetModelsAsync();
            if (this.FindName("ModelBox") is System.Windows.Controls.ComboBox cb)
            {
                cb.ItemsSource = models;
                if (!string.IsNullOrEmpty(Profile.Model) && models.Contains(Profile.Model))
                    cb.SelectedItem = Profile.Model;
                else
                    cb.Text = Profile.Model;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Profile.Name = NameBox.Text;
            if (this.FindName("ModelBox") is System.Windows.Controls.ComboBox cb)
            {
                Profile.Model = cb.SelectedItem?.ToString() ?? cb.Text ?? string.Empty;
            }
            else
            {
                try { Profile.Model = ModelBox.Text; } catch { Profile.Model = string.Empty; }
            }
            Profile.SystemPrompt = SystemBox.Text;
            Profile.DefaultPrompt = DefaultBox.Text;
            var oc2 = this.FindName("OCRBox") as System.Windows.Controls.TextBox;
            if (oc2 != null)
            {
                Profile.OCRPrompt = oc2.Text;
            }
            else
            {
                Profile.OCRPrompt = string.Empty;
            }
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