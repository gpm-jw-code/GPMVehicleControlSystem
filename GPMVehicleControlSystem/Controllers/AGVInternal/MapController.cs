using GPMVehicleControlSystem.Tools;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using AGVSystemCommonNet6.MAP;
using System.Net.NetworkInformation;

namespace GPMVehicleControlSystem.Controllers.AGVInternal
{
    [Route("api/[controller]")]
    [ApiController]
    public class MapController : ControllerBase
    {
        public static string Host = "192.168.0.1";
        public static string GetMapUrl => $"http://{Host}:6600/map/get";

        [HttpGet("GetMapFromServer")]
        public async Task<IActionResult> GetMap()
        {
            try
            {
                //先 ping 看看
                Ping ping = new Ping();
                PingReply? reply = ping.Send(Host, 1000);
                if (reply.Status != IPStatus.Success)
                {
                    return Ok(MapManager.LoadMapFromFile(Path.Combine(Environment.CurrentDirectory, "param/Map_UMTC_3F_Yellow.json")));
                }

                return Ok(await GetMapFromServer());
            }
            catch (Exception ex)
            {
                return Ok(MapManager.LoadMapFromFile(Path.Combine(Environment.CurrentDirectory, "param/Map_UMTC_3F_Yellow.json")));
            }
        }

        public static async Task<Map> GetMapFromServer()
        {

            Map? map = null;
            //先 ping 看看
            Ping ping = new Ping();
            PingReply? reply = ping.Send(Host, 300);
            if (reply.Status != IPStatus.Success)
            {
                throw new Exception("network error");
            }
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
