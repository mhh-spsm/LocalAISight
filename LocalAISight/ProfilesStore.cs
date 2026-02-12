using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LocalAISight.Models;

namespace LocalAISight
{
    public class ProfilesStore
    {
        private readonly ProfilesService _service = new ProfilesService();

        public static ProfilesStore Instance { get; } = new ProfilesStore();

        public List<Profile> Profiles { get; private set; } = new List<Profile>();

        public Profile ActiveProfile { get; private set; }

        public event Action? ActiveProfileChanged;

        private ProfilesStore() { }

        public async Task LoadAsync()
        {
            Profiles = await _service.LoadAsync();
            // restore active profile from settings if present
            string saved = string.Empty;
            try { saved = (string)Properties.Settings.Default["SelectedProfile"]; } catch { saved = string.Empty; }
            if (!string.IsNullOrEmpty(saved))
            {
                var found = Profiles.Find(p => p.Name == saved);
                if (found != null)
                {
                    // Use SetActive so the change is propagated
                    SetActive(found);
                }
            }
        }

        public async Task SaveAsync()
        {
            await _service.SaveAsync(Profiles);
        }

        public void SetActive(Profile? p)
        {
            ActiveProfile = p;
            try { Properties.Settings.Default["SelectedProfile"] = p?.Name ?? string.Empty; Properties.Settings.Default.Save(); } catch { }
            ActiveProfileChanged?.Invoke();
        }
    }
}