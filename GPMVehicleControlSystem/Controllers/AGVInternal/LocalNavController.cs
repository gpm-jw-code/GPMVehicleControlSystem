using AGVSystemCommonNet6.AGVMessage;
using Microsoft.AspNetCore.Mvc;
using AGVSystemCommonNet6.MAP;
using GPMVehicleControlSystem.Models;
using AGVSystemCommonNet6.AGVDispatch.Messages;
using GPMVehicleControlSystem.Models.VehicleControl;
using AGVSystemCommonNet6.Log;
using AGVSystemCommonNet6;

namespace GPMVehicleControlSystem.Controllers.AGVInternal
{
    [Route("api/[controller]")]
    [ApiController]
    public class LocalNavController : ControllerBase
    {
        private Vehicle agv => StaStored.CurrentVechicle;

        [HttpGet("Action")]
        public async Task<IActionResult> Action(ACTION_TYPE action, string? from, string? to = "", string? cst_id = "")
        {

            if (agv.Remote_Mode == REMOTE_MODE.ONLINE)
            {
                return Ok(new
                {
                    accpet = false,
                    error_message = $"AGV於 OFFLine 模式方可執行任務"
                });
            }

            if (agv.Sub_Status != AGVSystemCommonNet6.clsEnums.SUB_STATUS.IDLE)
            {
                return Ok(new
                {
                    accpet = false,
                    error_message = $"AGV當前狀態無法執行任務({agv.Sub_Status})"
                });
            }

            from = from == null ? "" : from;
            to = to == null ? "" : to;
            cst_id = cst_id == null ? "" : cst_id;

            int fromtag = -1;
            int totag = int.Parse(to);
            int currentTag = agv.Navigation.LastVisitedTag;

            if (action != ACTION_TYPE.Carry)
                fromtag = currentTag;
            else
                fromtag = int.Parse(from);
            Map? map = MapManager.LoadMapFromFile();
            try
            {
                map = await MapController.GetMapFromServer();
            }
            catch (Exception)
            {
            }
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
            taskDataDto = CreateMoveActionTaskJob(map, action, $"AGV_LOCAL_{DateTime.Now.ToString("yyyyMMdd_HHmmssffff")}", fromtag, int.Parse(to), 0);

            clsTaskDownloadData[]? taskLinkList = CreateActionLinksTaskJobs(map, action, fromtag, totag);

            if (taskLinkList.Length >= 1)
            {
                _ = Task.Run(async () =>
                  {
                      foreach (clsTaskDownloadData? _taskDataDto in taskLinkList)
                      {
                          if (agv.Sub_Status == clsEnums.SUB_STATUS.DOWN)
                              return;

                          agv.ExecuteAGVSTask(this, _taskDataDto);
                          await Task.Delay(200);
                          while (agv.CurrentTaskRunStatus != TASK_RUN_STATUS.ACTION_FINISH)
                          {
                              if (agv.Sub_Status == clsEnums.SUB_STATUS.DOWN)
                                  return;
                              await Task.Delay(200);
                          }
                          LOG.TRACE("Local WebUI Task Allocator : Next Task Will Start..");
                      }

                  });
                return Ok(new TaskActionResult
                {
                    accpet = true,
                    error_message = "",
                    path = taskLinkList.First().ExecutingTrajecory
                });
            }
            else
            {
                return Ok(new TaskActionResult
                {
                    accpet = false,
                    error_message = "Oppppps!",
                });
            }


        }


        private clsTaskDownloadData CreateMoveActionTaskJob(Map mapData, ACTION_TYPE actionType, string TaskName, int fromTag, int toTag, int Task_Sequence)
        {
            var pathFinder = new PathFinder();
            var pathPlanDto = pathFinder.FindShortestPathByTagNumber(mapData.Points, fromTag, toTag);
            clsTaskDownloadData actionData = new clsTaskDownloadData()
            {
                Action_Type = actionType,
                Destination = toTag,
                Task_Name = TaskName,
                Station_Type = STATION_TYPE.Normal,
                Task_Sequence = Task_Sequence,
                Task_Simplex = $"{TaskName}-{Task_Sequence}",
                Trajectory = pathFinder.GetTrajectory(mapData.Name, pathPlanDto.stations),
            };
            return actionData;
        }

        private clsTaskDownloadData[] CreateActionLinksTaskJobs(Map mapData, ACTION_TYPE actionType, int fromTag, int toTag)
        {
            string Task_Name = $"UI_{DateTime.Now.ToString("yyyyMMddHHmmssff")}";
            int seq = 1;
            PathFinder pathFinder = new PathFinder();
            int normal_move_start_tag;
            int normal_move_final_tag;
            List<clsTaskDownloadData> taskList = new List<clsTaskDownloadData>();

            MapStation? currentStation = mapData.Points.First(i => i.Value.TagNumber == agv.Navigation.LastVisitedTag).Value;
            MapStation? destineStation = mapData.Points.First(i => i.Value.TagNumber == toTag).Value;
            MapStation secondaryLocStation = mapData.Points[int.Parse(destineStation.Target.First().Key)];

            bool isInChargeOrEqPortStation = currentStation.StationType != STATION_TYPE.Normal;
            MapStation secondaryLocStation_of_chargeStateion = mapData.Points[int.Parse(currentStation.Target.First().Key)];

            if (isInChargeOrEqPortStation)
            {
                //Discharge
                clsTaskDownloadData homing_move_task = new clsTaskDownloadData
                {
                    Task_Name = Task_Name,
                    Task_Simplex = $"{Task_Name}-{seq}",
                    Task_Sequence = seq,
                    Action_Type = ACTION_TYPE.Discharge,
                    Destination = secondaryLocStation_of_chargeStateion.TagNumber,
                    Station_Type = secondaryLocStation_of_chargeStateion.StationType,
                    Homing_Trajectory = pathFinder.GetTrajectory(mapData.Name, new List<MapStation> { currentStation, secondaryLocStation_of_chargeStateion })
                };
                taskList.Add(homing_move_task);
                seq += 1;
            }


            normal_move_start_tag = isInChargeOrEqPortStation ? secondaryLocStation_of_chargeStateion.TagNumber : fromTag;

            if (actionType == ACTION_TYPE.None)
                normal_move_final_tag = toTag;
            else
            {
                normal_move_final_tag = secondaryLocStation.TagNumber;
            }

            //add normal 
            PathFinder.clsPathInfo? planPath = pathFinder.FindShortestPath(mapData.Points, isInChargeOrEqPortStation? secondaryLocStation_of_chargeStateion :  currentStation, actionType == ACTION_TYPE.None ? destineStation : secondaryLocStation);
            clsTaskDownloadData normal_move_task = new clsTaskDownloadData
            {
                Task_Name = Task_Name,
                Task_Simplex = $"{Task_Name}-{seq}",
                Task_Sequence = seq,
                Action_Type = ACTION_TYPE.None,
                Destination = normal_move_final_tag,
                Station_Type = STATION_TYPE.Normal,
                Trajectory = pathFinder.GetTrajectory(mapData.Name, planPath.stations)
            };
            taskList.Add(normal_move_task);
            seq += 1;

            if (actionType != ACTION_TYPE.None)
            {
                clsTaskDownloadData homing_move_task = new clsTaskDownloadData
                {
                    Task_Name = Task_Name,
                    Task_Simplex = $"{Task_Name}-{seq}",
                    Task_Sequence = seq,
                    Action_Type = actionType,
                    Destination = destineStation.TagNumber,
                    Station_Type = destineStation.StationType,
                    Homing_Trajectory = pathFinder.GetTrajectory(mapData.Name, new List<MapStation> { secondaryLocStation, destineStation })
                };
                taskList.Add(homing_move_task);
            }


            return taskList.ToArray();
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
