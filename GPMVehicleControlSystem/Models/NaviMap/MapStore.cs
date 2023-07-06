using AGVSystemCommonNet6.MAP;
using GPMVehicleControlSystem.Tools;
using Newtonsoft.Json;

namespace GPMVehicleControlSystem.Models.NaviMap
{
    public static class MapStore
    {
        public static string GetMapUrl
        {
            get
            {
                return AppSettingsHelper.GetValue<string>("VCS:Connections:AGVS:MapUrl");
            }
        }
        public static async Task<Map> GetMapFromServer()
        {
            Map? map = null;
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var response = await client.GetAsync(GetMapUrl);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var jsonStr = await response.Content.ReadAsStringAsync();
                        map = JsonConvert.DeserializeObject<Map>(jsonStr);
                    }
                }
                return (map);
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
    }
}
