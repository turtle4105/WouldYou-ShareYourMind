using System;
using System.Collections.Generic;
// 추가
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WouldYou_ShareMind.Models;

namespace WouldYou_ShareMind.Services
{
    /// <summary>
    /// %AppData%\WouldYou-ShareMind\settings.json 저장/로드
    /// </summary>
    public sealed class SettingsService : ISettingsService
    {
        private readonly string _filePath;
        public AppSettings Settings { get; } = new();

        public SettingsService()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "WouldYou-ShareMind");
            Directory.CreateDirectory(dir);
            _filePath = Path.Combine(dir, "settings.json");
        }

        public void Load()
        {
            if (!File.Exists(_filePath)) return;
            var json = File.ReadAllText(_filePath);
            var loaded = JsonSerializer.Deserialize<AppSettings>(json);
            if (loaded is null) return;

            Settings.ApiKey = loaded.ApiKey;
            Settings.EmpathyEndpoint = loaded.EmpathyEndpoint;
            Settings.SleepAudioPath = loaded.SleepAudioPath;
            Settings.MicDeviceNumber = loaded.MicDeviceNumber;
        }

        public async Task SaveAsync()
        {
            var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_filePath, json);
        }
    }
}
