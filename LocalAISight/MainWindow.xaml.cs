using LocalAISight.Models;
using Microsoft.Graphics.Canvas;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime; // KRÄVS för .AsBuffer()
using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Imaging;                      // För SoftwareBitmap
using Windows.Storage.Streams;                       // KRÄVS för InMemoryRandomAccessStream
using WinRT.Interop; // Detta sköts nu snyggare i WPF

namespace LocalAISight
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private OllamaClient _ollamaClient = new OllamaClient();
        private GraphicsCaptureItem item;
        private string lastImage;
        private List<string> _availableModels;
        public List<string> AvailableModels
        {
            get => _availableModels;
            set { _availableModels = value; OnPropertyChanged(nameof(AvailableModels)); }
        }

        private string _selectedModel;
        public string SelectedModel
        {
            get => _selectedModel;
            set { _selectedModel = value; OnPropertyChanged(nameof(SelectedModel)); }
        }
        // Use the shared ProfilesStore
        private readonly ProfilesStore _profilesStore = ProfilesStore.Instance;
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            _ = InitializeProfilesAsync();
            Loaded += MainWindow_Loaded; // 3) Ladda async efter att UI är klart
        }
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {

        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private async System.Threading.Tasks.Task InitializeProfilesAsync()
        {
            await _profilesStore.LoadAsync();
            ProfilesCombo.ItemsSource = null;
            ProfilesCombo.ItemsSource = _profilesStore.Profiles;
            // Set selection to active profile if any
            if (_profilesStore.ActiveProfile != null)
            {
                ProfilesCombo.SelectedItem = _profilesStore.ActiveProfile;
                UpdateUIFromActiveProfile();
                ProfilesCombo.Loaded += async (s, e) =>
                {

                };
            }
            AvailableModels = await _ollamaClient.GetModelsAsync(Properties.Settings.Default.UseExternalServer ? Properties.Settings.Default.ExternalIP : null);

            _profilesStore.ActiveProfileChanged += () =>
            {
                // update UI on active profile change
                Application.Current.Dispatcher.Invoke(UpdateUIFromActiveProfile);
            };
        }

        private async void UpdateUIFromActiveProfile()
        {
            var p = _profilesStore.ActiveProfile;
            if (p != null)
            {
                // reflect default prompt in UI

                //UserQuestionBox.Text = p.DefaultPrompt ?? string.Empty;
                // For now We leave the box empty so that the default question is used by default
//                OnPropertyChanged(nameof(AvailableModels));
                SelectedModel = p.Model ?? null;
            }
        }

        private void ManageProfilesButton_Click(object sender, RoutedEventArgs e)
        {
            var win = new ProfilesWindow();
            win.Owner = this;
            win.ShowDialog();
            // reload profiles into combo
            _ = InitializeProfilesAsync();
        }

        private void ProfilesCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProfilesCombo.SelectedItem is Profile p)
            {
                _profilesStore.SetActive(p);
            }
        }

        private async void TargetButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Skapa pickern
                var picker = new GraphicsCapturePicker();

                // 2. Hämta HWND för detta fönster och initiera pickern
                // Detta ersätter alla våra manuella COM-hacks!
                IntPtr hWnd = new WindowInteropHelper(this).Handle;
                InitializeWithWindow.Initialize(picker, hWnd);

                // 3. Visa dialogrutan
                item = await picker.PickSingleItemAsync();
                if (item == null) return;
                // --- HÄR KOLLAR VI UPPLÖSNINGEN ---
                int width = item.Size.Width;
                int height = item.Size.Height;

                if (width <= 0 || height <= 0)
                {
                    MessageBox.Show("Fönstret verkar vara minimerat eller dolt. Kan inte fånga en bild med 0 upplösning.");
                    return;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ett fel uppstod: {ex.Message}");
            }

        }
        private async void AskButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(lastImage))
            {
                MessageBox.Show("Ingen bild finns att beskriva än!");
                return;

            }
            try
            {
                var question = UserQuestionBox.Text;
                DescriptionLbl.Text = "AI:n tänker...";
                var result = await _ollamaClient.GetDescriptionAsync(lastImage, question, SelectedModel);
                DescriptionBox.Text = result;
                DescriptionLbl.Text = $"AI-Svar ({SelectedModel}):";
                DescriptionBox.Focus();

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ett fel uppstod: {ex.Message}");
            }

        }
        private void UserQuestionBox_GotFocus(object sender, RoutedEventArgs e)
        {
            // Markera all text när fältet får fokus (via Tab eller kod)
            UserQuestionBox.SelectAll();
        }
        private async void CaptureButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (item == null)
                {
                    MessageBox.Show("Välj ett fönster först!");
                    return;
                }
                // 4. Setup Grafik
                var canvasDevice = CanvasDevice.GetSharedDevice();
                var d3dDevice = WinRT.CastExtensions.As<IDirect3DDevice>(canvasDevice);
                // Visa upplösningen för användaren
                //                MessageBox.Show($"Fångar: {item.DisplayName}\nUpplösning: {width} x {height} pixlar");
                // 5. Fånga bild
                var question = UserQuestionBox.Text ?? "";
                string base64Image = await CaptureSingleFrameAsBase64(item, d3dDevice, true, true);
                lastImage = base64Image;
                DescriptionLbl.Text = "AI:n tänker...";
                var result = await _ollamaClient.GetDescriptionAsync(base64Image, question, SelectedModel);
                DescriptionBox.Text = result;
                DescriptionLbl.Text = $"AI-beskrivning ({SelectedModel}):";
                DescriptionBox.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ett fel uppstod: {ex.Message}");
            }

        }

        static async Task<string> CaptureSingleFrameAsBase64(GraphicsCaptureItem item, IDirect3DDevice device, bool useJpg = false, bool scaleDown = false)
        {
            var completionSource = new TaskCompletionSource<string>();
            var useEncoder = !useJpg ? BitmapEncoder.PngEncoderId : BitmapEncoder.JpegEncoderId;
            // Skapa framepool för en enda bild
            using var framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(
                device,
                Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized,
                1,
                item.Size);

            using var session = framePool.CreateCaptureSession(item);

            framePool.FrameArrived += async (sender, _) =>
            {
                using var frame = sender.TryGetNextFrame();
                if (frame == null) return;

                // Konvertera ytan till en SoftwareBitmap
                var softwareBitmap = await SoftwareBitmap.CreateCopyFromSurfaceAsync(frame.Surface);

                using var ms = new MemoryStream();
                var encoder = await BitmapEncoder.CreateAsync(useEncoder, ms.AsRandomAccessStream());
                if (scaleDown)
                {
                    // 2. Skala ner till t.ex. 1024px på bredden (Ministral-3 optimering)
                    float scale = Math.Min(1024f / item.Size.Width, 1024f / item.Size.Height);
                    if (scale < 1.0f)
                    {
                        encoder.BitmapTransform.ScaledWidth = (uint)(item.Size.Width * scale);
                        encoder.BitmapTransform.ScaledHeight = (uint)(item.Size.Height * scale);
                        encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Linear;
                    }
                }
                encoder.SetSoftwareBitmap(softwareBitmap);
                await encoder.FlushAsync();

                completionSource.TrySetResult(Convert.ToBase64String(ms.ToArray()));
            };

            session.StartCapture();
            var result = await completionSource.Task;
            session.Dispose();
            return result;
        }
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWin = new SettingsWindow();
            settingsWin.Owner = this; // Gör så fönstret hamnar ovanpå huvudfönstret
            settingsWin.ShowDialog(); // Öppnar som en modal dialog
        }
    }
}