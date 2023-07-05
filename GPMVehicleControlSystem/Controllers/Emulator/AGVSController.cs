using AGVSystemCommonNet6.AGVDispatch.Messages;
using GPMVehicleControlSystem.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GPMVehicleControlSystem.Controllers.Emulator
{
    [Route("api/[controller]")]
    [ApiController]
    public class AGVSController : ControllerBase
    {
        [HttpGet("TaskDownload")]
        public async Task<IActionResult> TaskDownload()
        {
            string task_name = $"Local_{DateTime.Now.ToString("yyyyMMdd_HHmmssffff")}";
            StaStored.CurrentVechicle.AGVS.OnTaskDownload.Invoke(new clsTaskDownloadData
            {
                Task_Name = task_name,
                Task_Sequence = 1,
                Destination = 7,
                Action_Type = ACTION_TYPE.None,
                Station_Type =STATION_TYPE.Normal,
                Trajectory = new clsMapPoint[]
                    {
                         new clsMapPoint
                         {
                              Point_ID =5,
                               X = -2.09,
                                Y = -7.91,
                                 Theta = 0,
                                  Speed = 1,
                         },
                          new clsMapPoint
                         {
                              Point_ID =7,
                               X = -2.04,
                                Y = -5.89,
                                 Theta = 0,
                                  Speed = 1,
                         }
                    }
            });

            return Ok();
        }
    }
}
