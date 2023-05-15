using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GPMVehicleControlSystem.Controllers.Emulator
{
    [Route("api/[controller]")]
    [ApiController]
    public class RosBridgeEmuController : ControllerBase
    {
        [HttpGet("/ws/ros")]
        public async Task Conn()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {

            }
            else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }
    }
}
