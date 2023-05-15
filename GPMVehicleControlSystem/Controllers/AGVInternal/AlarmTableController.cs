using GPMVehicleControlSystem.Models.Database;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GPMVehicleControlSystem.Controllers.AGVInternal
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class AlarmTableController : ControllerBase
    {
        [HttpGet("Clear")]
        public async Task<IActionResult> Clear()
        {
            return Ok(DBhelper.ClearAllAlarm());
        }

        [HttpGet("Query")]
        public async Task<IActionResult> QueryAlarmsByPage(int page, int page_size = 16)
        {
            return Ok(DBhelper.QueryAlarm(page, page_size));
        }
        [HttpGet("Total")]
        public async Task<IActionResult> Total()
        {
            return Ok(DBhelper.AlarmsTotalNum());
        }
    }
}
