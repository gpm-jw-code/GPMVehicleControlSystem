using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GPMVehicleControlSystem.Controllers
{
    [Route("/")]
    [ApiController]
    public class SPAController : ControllerBase
    {
        [HttpGet("/admin")]
        public async Task<ContentResult> Admin()
        {
            var html = System.IO.File.ReadAllText("wwwroot/index.html");
            return base.Content(html, "text/html");
        }
    }
}
