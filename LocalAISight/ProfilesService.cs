using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LocalAISight.Models;

namespace LocalAISight
{
    public class ProfilesService
    {
        private readonly string _path;
        private readonly JsonSerializerOptions _opts = new JsonSerializerOptions { WriteIndented = true };

        public ProfilesService()
        {
            var dir = Path.Combine(System.AppContext.BaseDirectory, "data");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            _path = Path.Combine(dir, "profiles.json");
        }

        public async Task<List<Profile>> LoadAsync()
        {
            if (!File.Exists(_path)) return new List<Profile>();
            var bytes = await File.ReadAllBytesAsync(_path);
            var txt = Encoding.UTF8.GetString(bytes);
            try
            {
                var list = JsonSerializer.Deserialize<List<Profile>>(txt, _opts);
                return list ?? new List<Profile>();
            }
            catch
            {
                return new List<Profile>();
            }
        }

        public async Task SaveAsync(List<Profile> profiles)
        {
            var txt = JsonSerializer.Serialize(profiles, _opts);
            var bytes = Encoding.UTF8.GetBytes(txt);
            await File.WriteAllBytesAsync(_path, bytes);
        }
    }
}