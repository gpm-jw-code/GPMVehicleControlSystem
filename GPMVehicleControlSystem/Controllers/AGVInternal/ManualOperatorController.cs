using GPMVehicleControlSystem.Models;
using GPMVehicleControlSystem.Models.VehicleControl;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GPMVehicleControlSystem.Controllers.AGVInternal
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ManualOperatorController : ControllerBase
    {
        private Vehicle agv => StaStored.CurrentVechicle;

        [HttpGet("Stop")]
        public async Task<IActionResult> Stop()
        {
            agv.ManualController?.Stop();
            return Ok();
        }
        [HttpGet("Forward")]
        public async Task<IActionResult> Forward(double speed = 0.08)
        {
             agv.ManualController?.Forward(speed);
            return Ok();
        }
        [HttpGet("Backward")]
        public async Task<IActionResult> Backward(double speed = 0.08)
        {
             agv.ManualController?.Backward(speed);
            return Ok();
        }


        [HttpGet("Right")]
        public async Task<IActionResult> Right(double speed = 0.1)
        {
             agv.ManualController?.TurnRight(speed);
            return Ok();
        }


        [HttpGet("Left")]
        public async Task<IActionResult> Left(double speed = 0.1)
        {
             agv.ManualController?.TurnLeft(speed);
            return Ok();
        }
    }
}
