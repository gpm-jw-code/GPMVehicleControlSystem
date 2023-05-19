using GPMVehicleControlSystem.Tools;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using AGVSystemCommonNet6.MAP;

namespace GPMVehicleControlSystem.Controllers.AGVInternal
{
    [Route("api/[controller]")]
    [ApiController]
    public class MapController : ControllerBase
    {

        public static string GetMapUrl = "http://192.168.0.1:6600/map/get";
        [HttpGet("GetMapFromServer")]
        public async Task<IActionResult> GetMapFromServer()
        {
            try
            {
                Map? map = null;
                using (HttpClient client = new HttpClient())
                {
                    var response = await client.GetAsync(GetMapUrl);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var jsonStr = await response.Content.ReadAsStringAsync();
                        map = JsonConvert.DeserializeObject<Map>(jsonStr);
                    }
                }
                return Ok(map);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

    }
}
