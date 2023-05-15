
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using GPMVehicleControlSystem.Models;
using GPMVehicleControlSystem.Models.AGVDispatch.Messages;
using GPMVehicleControlSystem.Models.VehicleControl;
using GPMVehicleControlSystem.Models.Buzzer;
using static GPMVehicleControlSystem.Models.VehicleControl.Vehicle;

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
            return Ok(StaStored.CurrentVechicle.CarController.IsConnected());
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
            return Ok(agv.Battery.Data);
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
            return Ok(await agv.Initialize());
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
            await agv.ResetAlarmsAsync();
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
            // var retcode = await agv.AGVSConnection.CarrierRemovedRequestAsync("", new string[] { agv.CSTReader.Data.data });
            return Ok(true);
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
            agv.Laser.ModeSwitch(mode);
            return Ok();
        }
    }
}
