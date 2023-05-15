using GPMVehicleControlSystem.Models.Emulators;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static GPMVehicleControlSystem.Models.VehicleControl.DIOModule.clsDIModule;

namespace GPMVehicleControlSystem.Controllers.Emulator
{
    [Route("api/[controller]")]
    [ApiController]
    public class WagoEmuController : ControllerBase
    {
        [HttpGet("SetInput")]
        public async Task<IActionResult> SetInput(DI_ITEM Input, bool State)
        {
            StaEmuManager.wagoEmu.SetInput(Input, State);
            return Ok(new { Address = Input.ToString(), State });
        }



        [HttpGet("Emo")]
        public async Task<IActionResult> Emo()
        {
            StaEmuManager.wagoEmu.SetInput(DI_ITEM.EMO, false);
            return Ok();
        }



        [HttpGet("ResetButton")]
        public async Task<IActionResult> ResetButton()
        {
            StaEmuManager.wagoEmu.SetInput(DI_ITEM.Panel_Reset_PB, true);
            await Task.Delay(500);
            StaEmuManager.wagoEmu.SetInput(DI_ITEM.Panel_Reset_PB, false);
            StaEmuManager.wagoEmu.SetInput(DI_ITEM.EMO, true);
            return Ok();
        }
    }
}
