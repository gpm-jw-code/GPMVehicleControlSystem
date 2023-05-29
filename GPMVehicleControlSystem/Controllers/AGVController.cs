using AGVSystemCommonNet6;
using AGVSystemCommonNet6.AGVDispatch.Messages;
using AGVSystemCommonNet6.HttpHelper;
using GPMVehicleControlSystem.Models;
using GPMVehicleControlSystem.Models.VehicleControl;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RosSharp.RosBridgeClient;

namespace GPMVehicleControlSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AGVController : ControllerBase
    {

        private Vehicle agv;
        public AGVController()
        {

            this.agv = StaStored.CurrentVechicle;
        }

        [HttpGet("RunningState")]
        public async Task<IActionResult> GetRunningStatus()
        {
            var state = agv.GenRunningStateReportData();
            return Ok(state.ToJson());
        }

        [HttpGet("OnlineState")]
        public async Task<IActionResult> GetOnlineState()
        {
            return Ok(agv.Remote_Mode);
        }


        [HttpGet("agv_online")]
        public async Task<IActionResult> agv_online()
        {
            if (agv.Sub_Status != clsEnums.SUB_STATUS.IDLE)
            {
                return Ok(new clsAPIRequestResult { Success = false, Message = $"當前狀態不可上線({agv.Sub_Status})" });
            }
            agv.Remote_Mode = REMOTE_MODE.ONLINE;
            return Ok(new clsAPIRequestResult { Success = true });
        }


        [HttpGet("agv_offline")]
        public async Task<IActionResult> agv_offline()
        {
            agv.Remote_Mode = REMOTE_MODE.OFFLINE;
            return Ok(new { Success = true, Message = "" });
        }

    }
}
