using GPMVehicleControlSystem.Models.Buzzer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GPMVehicleControlSystem.Controllers.AGVInternal
{
    [Route("api/[controller]")]
    [ApiController]
    public class SoundsController : ControllerBase
    {
        [HttpGet("Alarm")]
        public async Task<IActionResult> Alarm()
        {
            BuzzerPlayer.BuzzerAlarm();
            return Ok();
        }
        [HttpGet("Moving")]
        public async Task<IActionResult> Moving()
        {
            BuzzerPlayer.BuzzerMoving();
            return Ok();
        }
        [HttpGet("Action")]
        public async Task<IActionResult> Action()
        {
            BuzzerPlayer.BuzzerAction();
            return Ok();
        }
        [HttpGet("Stop")]
        public async Task<IActionResult> Stop()
        {
            BuzzerPlayer.BuzzerStop();
            return Ok();
        }
    }
}
