
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using GPMVehicleControlSystem.Models;
using GPMVehicleControlSystem.Models.VehicleControl;
using GPMVehicleControlSystem.Models.Buzzer;
using static GPMVehicleControlSystem.Models.VehicleControl.Vehicle;
using AGVSystemCommonNet6.AGVDispatch.Messages;
using static AGVSystemCommonNet6.clsEnums;
using AGVSystemCommonNet6.GPMRosMessageNet.Messages;

namespace GPMVehicleControlSystem.Controllers.AGVInternal
{

    [Route("api/[controller]")]
    [ApiController]
    public class VMSController : ControllerBase
    {

        private Vehicle agv => StaStored.CurrentVechicle;

        [HttpGet("Where_r_u")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task Where_r_u()
        {
            await Task.Delay(1);
            _ = Task.Factory.StartNew(async () =>
            {
                for (int i = 0; i < 3; i++)
                {
                    agv.DirectionLighter.OpenAll();
                    Thread.Sleep(150);
                    agv.DirectionLighter.CloseAll();
                    Thread.Sleep(150);
                }
            });
            return;
        }



        [HttpGet("AutoMode")]
        public async Task<IActionResult> AutoModeSwitch(OPERATOR_MODE mode)
        {
            bool confirm = await agv.Auto_Mode_Siwtch(mode);
            return Ok(confirm);
        }

        [HttpGet("OnlineMode")]
        public async Task<IActionResult> OnlineModeSwitch(REMOTE_MODE mode)
        {
            try
            {
                if (agv.BarcodeReader.CurrentTag == 0 && mode == REMOTE_MODE.ONLINE)
                {
                    return Ok(new
                    {
                        Success = false,
                        Message = "上線時車子必須停在Tag上."
                    });
                }
                (bool success, RETURN_CODE return_code) result = await agv.Online_Mode_Switch(mode);
                return Ok(new
                {
                    Success = result.success,
                    Message = $"Code Error:{result.return_code}"
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    Success = false,
                    Message = $"Code Error:{ex.Message}"
                });
            }
        }


        [HttpGet("ROSConnected")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> ROSConnected()
        {
            await Task.Delay(1);
            return Ok(StaStored.CurrentVechicle.AGVC.IsConnected());
        }

        [HttpGet("Mileage")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> Mileage()
        {
            await Task.Delay(1);
            return Ok(agv.Odometry);
        }
        [HttpGet("BateryState")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> BateryState()
        {
            await Task.Delay(1);
            if (agv.Batteries.Count == 0)
                return Ok(new BatteryState
                {
                    batteryLevel = 0,
                });
            return Ok(agv.Batteries.Values.First().Data);
        }

        [HttpPost("EMO")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> EMO()
        {
            agv.SoftwareEMO();
            return Ok("OK");
        }

        [HttpPost("Initialize")]
        public async Task<IActionResult> Initialize()
        {
            var result = await agv.Initialize();
            return Ok(new {confirm =result.confirm,message = result.message });
        }


        [HttpGet("CancelInitProcess")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> CancelInitProcess()
        {
            await Task.Delay(1);
            await agv.CancelInitialize();
            //bool setTagIDSuccess = await VMSEntity.Initializer.Initial_Robot_Pose_with_Tag();
            return Ok(true);
        }


        [HttpPost("ResetAlarm")]
        public async Task<IActionResult> ResetAlarm()
        {
            await Task.Delay(1);
            await agv.ResetAlarmsAsync(false);
            return Ok("OK");
        }

        [HttpPost("BuzzerOff")]
        public async Task<IActionResult> BuzzerOFF()
        {
            await Task.Delay(1);
            BuzzerPlayer.BuzzerStop();
            return Ok("OK");
        }

        [HttpPost("RemoveCassette")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> RemoveCassette()
        {
            await Task.Delay(1);
            var retcode = await agv.RemoveCstData();
            return Ok(retcode == RETURN_CODE.OK);
        }



        [HttpGet("DIO/DO_State")]
        [ApiExplorerSettings(IgnoreApi = false)]
        public async Task<IActionResult> DO_State(string address, bool state)
        {
            await Task.Delay(1);
            agv.WagoDO.SetState(address, state);
            return Ok(true);
        }

        [HttpGet("DIO/DI_State")]
        [ApiExplorerSettings(IgnoreApi = false)]
        public async Task<IActionResult> DI_State(string address, bool state)
        {
            await Task.Delay(1);
            agv.WagoDI.SetState(address, state);
            return Ok();
        }


        [HttpGet("LaserMode")]
        public async Task<IActionResult> LaserMode(int mode)
        {
            await Task.Delay(1);
            await agv.Laser.ModeSwitch(mode);
            return Ok();
        }



        [HttpGet("TriggerCSTReader")]
        public async Task<IActionResult> TriggerCSTReader()
        {
            (bool request_success, bool action_done) ret = await agv.AGVC.TriggerCSTReader();
            string barcode = "ERROR";
            if (ret.action_done)
            {
                barcode = agv.CSTReader.Data.data;
            }
            return Ok(new { barcode });
        }


        [HttpGet("StopCSTReader")]
        public async Task<IActionResult> StopCSTReader()
        {
            _ = Task.Factory.StartNew(async () =>
            {
                (bool request_success, bool action_done) ret = await agv.AGVC.AbortCSTReader();
            });
            return Ok();
        }

        [HttpGet("RunningStatus")]
        public async Task<IActionResult> GetRunningStatus()
        {
            return Ok(agv.GenRunningStateReportData());
        }
    }
}
