using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yedekleme
{
    public class IniFile
    {
        private readonly Dictionary<string, Dictionary<string, string>> data;

        public IniFile()
        {
            data = new Dictionary<string, Dictionary<string, string>>();
        }

        public void Load(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("INI file not found", path);

            string currentSection = null;
            foreach (var line in File.ReadAllLines(path))
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith(";"))
                    continue;

                if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                {
                    currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2);
                    if (!data.ContainsKey(currentSection))
                        data[currentSection] = new Dictionary<string, string>();
                }
                else if (currentSection != null)
                {
                    var keyValue = trimmedLine.Split(new[] { '=' }, 2);
                    if (keyValue.Length == 2)
                    {
                        data[currentSection][keyValue[0].Trim()] = keyValue[1].Trim();
                    }
                }
            }
        }

        public string GetValue(string section, string key, string defaultValue = null)
        {
            if (data.TryGetValue(section, out var sectionData) && sectionData.TryGetValue(key, out var value))
                return value;
            return defaultValue;
        }

        public void SetValue(string section, string key, string value)
        {
            if (!data.ContainsKey(section))
                data[section] = new Dictionary<string, string>();
            data[section][key] = value;
        }

        public void Save(string path)
        {
            using (var writer = new StreamWriter(path))
            {
                foreach (var section in data)
                {
                    writer.WriteLine($"[{section.Key}]");
                    foreach (var keyValue in section.Value)
                    {
                        writer.WriteLine($"{keyValue.Key}={keyValue.Value}");
                    }
                    writer.WriteLine();
                }
            }
        }
    }

}
