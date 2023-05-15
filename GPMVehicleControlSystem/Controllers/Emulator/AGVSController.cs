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
            StaStored.CurrentVechicle.AGVSConnection.OnTaskDownload.Invoke(new Models.AGVDispatch.Messages.clsTaskDownloadData
            {
                Task_Name = task_name,
                Task_Simplex = task_name + "_1",
                Task_Sequence = 1,
                Destination = 7,
                Action_Type = "None",
                Station_Type = Models.AGVDispatch.Messages.STATION_TYPE.Normal,
                Trajectory = new Models.AGVDispatch.Messages.clsMapPoint[]
                    {
                         new Models.AGVDispatch.Messages.clsMapPoint
                         {
                              Point_ID =5,
                               X = -2.09,
                                Y = -7.91,
                                 Theta = 0,
                                  Speed = 1,
                         },
                          new Models.AGVDispatch.Messages.clsMapPoint
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
