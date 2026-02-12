using System.Collections.Generic;
using System.Windows;
using LocalAISight.Models;

namespace LocalAISight
{
    public partial class ProfilesWindow : Window
    {
        private readonly ProfilesService _service = new ProfilesService();
        private List<Profile> _profiles = new List<Profile>();

        public ProfilesWindow()
        {
            InitializeComponent();
            _ = LoadProfiles();
        }

        private async System.Threading.Tasks.Task LoadProfiles()
        {
            _profiles = await _service.LoadAsync();
            ProfilesList.ItemsSource = null;
            ProfilesList.ItemsSource = _profiles;
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var editor = new ProfileEditorWindow();
            editor.Owner = this;
            if (editor.ShowDialog() == true)
            {
                _profiles.Add(editor.Profile);
                await _service.SaveAsync(_profiles);
                await LoadProfiles();
            }
        }

        private async void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProfilesList.SelectedItem is Profile p)
            {
                var editor = new ProfileEditorWindow(p);
                editor.Owner = this;
                if (editor.ShowDialog() == true)
                {
                    await _service.SaveAsync(_profiles);
                    await LoadProfiles();
                }
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProfilesList.SelectedItem is Profile p)
            {
                if (MessageBox.Show($"Radera profil '{p.Name}'?","Bekrafta",MessageBoxButton.YesNo)==MessageBoxResult.Yes)
                {
                    _profiles.Remove(p);
                    await _service.SaveAsync(_profiles);
                    await LoadProfiles();
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}