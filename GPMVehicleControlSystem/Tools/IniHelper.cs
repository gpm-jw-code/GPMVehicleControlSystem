using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;

namespace GPMVehicleControlSystem.Tools
{
    public class IniHelper
    {
        private ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
        private IConfigurationRoot configuration;
        public string FilePath { get; }
        public IniHelper(string iniFilePath)
        {
            FilePath = iniFilePath;
            configurationBuilder.AddIniFile(iniFilePath);
            configuration = configurationBuilder.Build();

        }
        public bool SetValue(string section, string key, string value, out string error_msg)
        {
            error_msg = "";
            try
            {
                if (configuration == null)
                {
                    error_msg = "configuration is null";
                    return false;
                }

                configuration.GetSection(section)[key] = value;
                var content = File.ReadAllText(FilePath);
                var regex = new Regex($@"^{key}\s*=\s*(.*)$", RegexOptions.Multiline);
                var replacement = $"{key}={value}";
                content = regex.Replace(content, replacement);

                File.WriteAllText(FilePath, content);
                //}
                return true;
            }
            catch (Exception ex)
            {
                error_msg = ex.Message;
                return false;
            }

        }
        public string GetValue(string section, string key)
        {
            if (configuration == null)
            {
                return "";
            }
            else
            {   //12,23#ddd
                string str = configuration[$"{section}:{key}"];
                if (str == null)
                {
                    return "";
                }
                if (str.Contains("#"))
                {
                    int comment_char_start_index = str.IndexOf('#');
                    return str.Substring(0, comment_char_start_index).TrimEnd();
                }
                else
                {
                    return str.TrimEnd();
                }
            }
        }
    }
}
