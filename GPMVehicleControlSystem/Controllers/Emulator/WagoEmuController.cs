using GPMVehicleControlSystem.Models.Emulators;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static GPMVehicleControlSystem.VehicleControl.DIOModule.clsDIModule;

namespace GPMVehicleControlSystem.Controllers.Emulator
{
    [Route("api/[controller]")]
    [ApiController]
    public class WagoEmuController : ControllerBase
    {
        [HttpGet("SetInput")]
        public async Task<IActionResult> SetInput(DI_ITEM Input, bool State)
        {
            StaEmuManager.wagoEmu.SetState(Input, State);
            return Ok(new { Address = Input.ToString(), State });
        }



        [HttpGet("Emo")]
        public async Task<IActionResult> Emo()
        {
            StaEmuManager.wagoEmu.SetState(DI_ITEM.EMO, false);
            return Ok();
        }



        [HttpGet("Horizon_Moto_Switch")]
        public async Task<IActionResult> Horizon_Motor_Switch(bool state)
        {
            StaEmuManager.wagoEmu.SetState(DI_ITEM.Horizon_Motor_Switch, state);
            return Ok();
        }


        [HttpGet("ResetButton")]
        public async Task<IActionResult> ResetButton()
        {
            StaEmuManager.wagoEmu.SetState(DI_ITEM.Panel_Reset_PB, true);
            await Task.Delay(500);
            StaEmuManager.wagoEmu.SetState(DI_ITEM.Panel_Reset_PB, false);
            StaEmuManager.wagoEmu.SetState(DI_ITEM.EMO, true);
            return Ok();
        }
    }
}
