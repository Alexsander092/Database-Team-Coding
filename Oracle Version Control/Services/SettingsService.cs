using System.Text;
using System.Reflection;

namespace Oracle_Version_Control.Services
{
    public class SettingsService
    {
        private readonly string _settingsPath;
        private readonly Dictionary<string, string> _settings = new Dictionary<string, string>();

        public SettingsService()
        {
            string appDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            _settingsPath = Path.Combine(appDirectory, "settings.ini");
            LoadSettings();
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    string[] lines = File.ReadAllLines(_settingsPath);
                    foreach (string line in lines)
                    {
                        string trimmedLine = line.Trim();
                        if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(";") || trimmedLine.StartsWith("#"))
                            continue;

                        int equalPos = trimmedLine.IndexOf('=');
                        if (equalPos > 0)
                        {
                            string key = trimmedLine.Substring(0, equalPos).Trim();
                            string value = trimmedLine.Substring(equalPos + 1).Trim();
                            _settings[key] = value;
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        public bool SaveSettings()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("; Oracle Version Control Settings");
                sb.AppendLine($"; Generated on {DateTime.Now}");
                sb.AppendLine();

                foreach (var kvp in _settings)
                {
                    sb.AppendLine($"{kvp.Key}={kvp.Value}");
                }

                string directory = Path.GetDirectoryName(_settingsPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                File.WriteAllText(_settingsPath, sb.ToString());
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public string GetSetting(string key, string defaultValue = "")
        {
            if (_settings.TryGetValue(key, out string value))
            {
                return value;
            }
            return defaultValue;
        }

        public void SetSetting(string key, string value)
        {
            _settings[key] = value;
        }

        public bool HasSetting(string key)
        {
            return _settings.ContainsKey(key);
        }
    }
}