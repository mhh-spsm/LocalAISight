using System.Configuration;
using System.Data;
using System.Windows;

namespace LocalAISight
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // Load profiles and activate saved profile before main window is shown
            await ProfilesStore.Instance.LoadAsync();
        }
    }

}
