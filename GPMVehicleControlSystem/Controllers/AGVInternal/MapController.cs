using GPMVehicleControlSystem.Tools;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using AGVSystemCommonNet6.MAP;
using System.Net.NetworkInformation;
using GPMVehicleControlSystem.Models.NaviMap;
using GPMVehicleControlSystem.Models;

namespace GPMVehicleControlSystem.Controllers.AGVInternal
{
    [Route("api/[controller]")]
    [ApiController]
    public class MapController : ControllerBase
    {

        [HttpGet("GetMapFromServer")]
        public async Task<IActionResult> GetMap()
        {
            try
            {
                return Ok(StaStored.CurrentVechicle.NavingMap);
            }
            catch (Exception ex)
            {
                return Ok(MapManager.LoadMapFromFile(Path.Combine(Environment.CurrentDirectory, "param/Map_UMTC_3F_Yellow.json")));
            }
        }
    }
}
