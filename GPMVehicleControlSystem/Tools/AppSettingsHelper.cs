using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace GPMVehicleControlSystem.Tools
{
    public class AppSettingsHelper
    {
        private static IConfiguration configuration
        {
            get
            {
                try
                {

                    var configBuilder = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json");
                    var configuration = configBuilder.Build();
                    return configuration;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }
        public static T GetValue<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(string key)
        {
            return configuration.GetValue(key, default(T));
        }
    }
}
