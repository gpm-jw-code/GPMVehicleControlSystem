using AGVSystemCommonNet6.Alarm.VMS_ALARM;
using GPMVehicleControlSystem.Models;
using GPMVehicleControlSystem.Models.Emulators;
using GPMVehicleControlSystem.Models.VehicleControl;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static GPMVehicleControlSystem.VehicleControl.DIOModule.clsDIModule;
using static GPMVehicleControlSystem.VehicleControl.DIOModule.clsDOModule;

namespace GPMVehicleControlSystem.Controllers.Emulator
{
    [Route("api/[controller]")]
    [ApiController]
    public class EQHandshakeEmuController : ControllerBase
    {
        private Vehicle agv => StaStored.CurrentVechicle;

        [HttpGet("EQAlarmWhenEQBusySimulation")]
        public async Task<IActionResult> EQAlarmWhenEQBusySimulation()
        {
            StaEmuManager.wagoEmu.SetState(DI_ITEM.EQ_L_REQ, true);
            StaEmuManager.wagoEmu.SetState(DI_ITEM.EQ_BUSY, true);
            agv.WagoDO.SetState(DO_ITEM.EMU_EQ_L_REQ, true);
            agv.WagoDO.SetState(DO_ITEM.EMU_EQ_BUSY, true);
            agv.EQAlarmWhenEQBusyFlag = true;
            AlarmManager.AddAlarm( AlarmCodes.Handshake_Fail_Inside_EQ_EQ_GO, false);
            agv.Sub_Status = AGVSystemCommonNet6.clsEnums.SUB_STATUS.DOWN;
            return Ok();
        }
        

        [HttpGet("EQInitialze")]
        public async Task<IActionResult> EQInitialze()
        {
            StaEmuManager.wagoEmu.SetState(DI_ITEM.EQ_L_REQ, false);
            StaEmuManager.wagoEmu.SetState(DI_ITEM.EQ_U_REQ, false);
            StaEmuManager.wagoEmu.SetState(DI_ITEM.EQ_BUSY, false);
            StaEmuManager.wagoEmu.SetState(DI_ITEM.EQ_READY, false);
            agv.WagoDO.SetState(DO_ITEM.EMU_EQ_L_REQ, false);
            agv.WagoDO.SetState(DO_ITEM.EMU_EQ_U_REQ, false);
            agv.WagoDO.SetState(DO_ITEM.EMU_EQ_BUSY, false);
            agv.WagoDO.SetState(DO_ITEM.EMU_EQ_READY, false);
            agv.EQAlarmWhenEQBusyFlag = false;
            return Ok();
        }
    }
}
