using AGVSystemCommonNet6.AGVMessage;
using Microsoft.AspNetCore.Mvc;
using AGVSystemCommonNet6.MAP;
using GPMVehicleControlSystem.Models;
using AGVSystemCommonNet6.AGVDispatch.Messages;
using GPMVehicleControlSystem.Models.VehicleControl;

namespace GPMVehicleControlSystem.Controllers.AGVInternal
{
    [Route("api/[controller]")]
    [ApiController]
    public class LocalNavController : ControllerBase
    {
        private Vehicle agv => StaStored.CurrentVechicle;

        [HttpGet("Action")]
        public async Task<IActionResult> Action(string action, string? from, string? to = "", string? cst_id = "")
        {

            if (agv.Sub_Status != AGVSystemCommonNet6.clsEnums.SUB_STATUS.IDLE)
            {
                return Ok(new
                {
                    accpet = false,
                    error_message = $"AGV當前狀態無法執行任務({agv.Sub_Status})"
                });
            }
            if (agv.Remote_Mode == REMOTE_MODE.ONLINE)
            {
                return Ok(new
                {
                    accpet = false,
                    error_message = $"AGV於 OFFLine 模式方可執行任務"
                });
            }

            from = from == null ? "" : from;
            to = to == null ? "" : to;
            cst_id = cst_id == null ? "" : cst_id;

            int fromtag = -1;
            int totag = int.Parse(to);
            int currentTag = agv.Navigation.LastVisitedTag;

            if (action != "carry")
                fromtag = currentTag;
            else
                fromtag = int.Parse(from);

            Map? map = MapManager.LoadMapFromFile(Path.Combine(Environment.CurrentDirectory, "param/Map_UMTC_3F_Yellow.json"));
            var fromStationFound = map.Points.Values.ToList().FirstOrDefault(st => st.TagNumber == fromtag);
            var toStationFound = map.Points.Values.ToList().FirstOrDefault(st => st.TagNumber == totag);

            if (fromStationFound == null)
            {
                return Ok(new TaskActionResult
                {
                    accpet = false,
                    error_message = $"在圖資中找不到Tag為{fromtag}的站點"
                });
            }
            if (toStationFound == null)
            {
                return Ok(new TaskActionResult
                {
                    accpet = false,
                    error_message = $"在圖資中找不到Tag為{totag}的站點"
                });
            }

            clsTaskDownloadData taskDataDto = null;
            if (action == "move")
            {
                taskDataDto = CreateMoveActionTaskJob(map, $"AGV_LOCAL_{DateTime.Now.ToString("yyyyMMdd_HHmmssffff")}", fromtag, int.Parse(to), 0);
            }


            if (action != null)
            {
                agv.ExecuteAGVSTask(this, taskDataDto);
                return Ok(new TaskActionResult
                {
                    accpet = true,
                    error_message = "",
                    path = taskDataDto.ExecutingTrajecory
                });
            }
            else
            {
                return Ok(new TaskActionResult
                {
                    accpet = false,
                    error_message = ""
                });
            }

        }

        private clsTaskDownloadData CreateMoveActionTaskJob(Map mapData, string TaskName, int fromTag, int toTag, int Task_Sequence)
        {
            var pathFinder = new AGVSystemCommonNet6.MAP.PathFinder();
            var pathPlanDto = pathFinder.FindShortestPathByTagNumber(mapData.Points, fromTag, toTag);
            clsTaskDownloadData actionData = new clsTaskDownloadData()
            {
                Action_Type = ACTION_TYPE.None,
                Destination = toTag,
                Task_Name = TaskName,
                Station_Type = STATION_TYPE.Normal,
                Task_Sequence = Task_Sequence,
                Task_Simplex = $"{TaskName}-{Task_Sequence}",
                Trajectory = pathFinder.GetTrajectory(mapData.Name, pathPlanDto.stations),
            };
            return actionData;
        }



        public class TaskActionResult
        {
            public string agv_name { get; set; } = StaStored.CurrentVechicle.CarName;
            public bool accpet { get; set; }
            public string error_message { get; set; } = "";
            public clsMapPoint[] path { get; set; } = new clsMapPoint[0];

        }

    }
}
