using AGVSystemCommonNet6.AGVDispatch.Messages;
using AGVSystemCommonNet6.Alarm.VMS_ALARM;
using AGVSystemCommonNet6.GPMRosMessageNet.Messages;
using AGVSystemCommonNet6.HttpHelper;
using AGVSystemCommonNet6.Log;
using AGVSystemCommonNet6.TASK;
using GPMVehicleControlSystem.Models.VehicleControl.TaskExecute;
using static AGVSystemCommonNet6.clsEnums;

namespace GPMVehicleControlSystem.Models.VehicleControl
{
    public partial class Vehicle
    {
        public TASK_RUN_STATUS CurrentTaskRunStatus = TASK_RUN_STATUS.NO_MISSION;
        public enum HS_METHOD
        {
            E84, MODBUS, EMULATION
        }

        public TaskBase ExecutingTask;

        public HS_METHOD Hs_Method = HS_METHOD.EMULATION;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="taskDownloadData"></param>
        /// <returns></returns>
        internal bool AGVSTaskDownloadConfirm(clsTaskDownloadData taskDownloadData)
        {
            AGV_Reset_Flag = false;

            if (Main_Status == MAIN_STATUS.DOWN) //TODO More Status Confirm when recieve AGVS Task
                return false;

            return true;
        }
        Dictionary<string, List<int>> TaskTrackingTags = new Dictionary<string, List<int>>();

        /// <summary>
        /// 執行派車系統任務
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="taskDownloadData"></param>
        internal void ExecuteAGVSTask(object? sender, clsTaskDownloadData taskDownloadData)
        {
            CurrentTaskRunStatus = TASK_RUN_STATUS.WAIT;
            LOG.INFO($"Task Download: Task Name = {taskDownloadData.Task_Name} , Task Simple = {taskDownloadData.Task_Simplex}");
            ACTION_TYPE action = taskDownloadData.Action_Type;

            if (!TaskTrackingTags.TryAdd(taskDownloadData.Task_Simplex, taskDownloadData.TagsOfTrajectory))
            {
                if (taskDownloadData.TagsOfTrajectory.Count != 1)
                    TaskTrackingTags[taskDownloadData.Task_Simplex] = taskDownloadData.TagsOfTrajectory;
            }

            Task.Run(async () =>
            {
                if (AGVC.IsAGVExecutingTask)
                {
                    LOG.INFO($"在 TAG {BarcodeReader.CurrentTag} 收到新的路徑擴充任務");
                    await ExecutingTask.AGVSPathExpand(taskDownloadData);
                }
                else
                {
                    if (ExecutingTask != null)
                    {
                        if (ExecutingTask.RunningTaskData.TagsOfTrajectory.Count != taskDownloadData.TagsOfTrajectory.Count)
                        {

                        }
                    }

                    clsTaskDownloadData _taskDownloadData;
                    _taskDownloadData = taskDownloadData;

                    if (action == ACTION_TYPE.None)
                        ExecutingTask = new NormalMoveTask(this, _taskDownloadData);
                    else if (action == ACTION_TYPE.Charge)
                        ExecutingTask = new ChargeTask(this, _taskDownloadData);
                    else if (action == ACTION_TYPE.Discharge)
                        ExecutingTask = new DischargeTask(this, _taskDownloadData);
                    else if (action == ACTION_TYPE.Load)
                        ExecutingTask = new LoadTask(this, _taskDownloadData);
                    else if (action == ACTION_TYPE.Unload)
                        ExecutingTask = new UnloadTask(this, _taskDownloadData);
                    else if (action == ACTION_TYPE.Park)
                        ExecutingTask = new ParkTask(this, _taskDownloadData);
                    else
                    {
                        throw new NotImplementedException();
                    }

                    Sub_Status = SUB_STATUS.RUN;
                    await Task.Delay(500);
                    ExecutingTask.OnTaskFinish = async (task_name) =>
                    {
                        await Task.Delay(5000);
                        TaskTrackingTags.Remove(task_name);
                    };
                    await ExecutingTask.Execute();

                }
            });
        }



        private void OnTagLeaveHandler(object? sender, int leaveTag)
        {
            if (Operation_Mode == OPERATOR_MODE.MANUAL)
                return;

            if (ExecutingTask.action == ACTION_TYPE.None)
                Laser.ApplyAGVSLaserSetting();

        }
        private void OnTagReachHandler(object? sender, int currentTag)
        {
            if (Operation_Mode == OPERATOR_MODE.MANUAL)
                return;
            if (ExecutingTask == null)
                return;
            var TagPoint = ExecutingTask.RunningTaskData.ExecutingTrajecory.FirstOrDefault(pt => pt.Point_ID == currentTag);
            if (TagPoint == null)
            {
                LOG.Critical($"AGV抵達 {currentTag} 但在任務軌跡上找不到該站點。");
                return;
            }
            PathInfo? pathInfoRos = ExecutingTask.RunningTaskData.RosTaskCommandGoal?.pathInfo.FirstOrDefault(path => path.tagid == TagPoint.Point_ID);
            if (pathInfoRos == null)
            {
                AGVC.AbortTask();
                AlarmManager.AddAlarm(AlarmCodes.Motion_control_Tracking_Tag_Not_On_Tag_Or_Tap, true);
                Sub_Status = SUB_STATUS.DOWN;
                return;
            }
            Laser.AgvsLsrSetting = TagPoint.Laser;

            LOG.INFO($"AGV抵達 Tag {currentTag},派車雷射設定:{Laser.AgvsLsrSetting}");
            if (ExecutingTask.RunningTaskData.TagsOfTrajectory.Last() != Navigation.LastVisitedTag)
                FeedbackTaskStatus(TASK_RUN_STATUS.NAVIGATING);
        }


        internal bool AGVSTaskResetReqHandle(RESET_MODE mode)
        {
            AlarmManager.AddAlarm(AlarmCodes.AGVs_Abort_Task);
            AGV_Reset_Flag = true;
            Task.Factory.StartNew(async () =>
            {
                AGVC.AbortTask(mode);
                await FeedbackTaskStatus(TASK_RUN_STATUS.ACTION_FINISH);
            });
            Sub_Status = SUB_STATUS.ALARM;
            ExecutingTask.Abort();
            return true;
        }

        internal async Task FeedbackTaskStatus(TASK_RUN_STATUS status)
        {
            CurrentTaskRunStatus = status;
            await Task.Delay(1000);
            //if (Remote_Mode == REMOTE_MODE.OFFLINE)
            //    return;

            if (VmsProtocol == VMS_PROTOCOL.TCPIP)
                await AGVS.TryTaskFeedBackAsync(ExecutingTask.RunningTaskData, GetCurrentTagIndexOfTrajectory(), status, Navigation.LastVisitedTag);
            else
                await FeedbackTaskStatusViaHttp(ExecutingTask.RunningTaskData, GetCurrentTagIndexOfTrajectory(), status);

            //if (status == TASK_RUN_STATUS.ACTION_FINISH)
            //    CurrentTaskRunStatus = TASK_RUN_STATUS.NO_MISSION;
        }

        private async Task FeedbackTaskStatusViaHttp(clsTaskDownloadData runningTaskData, int pointIndex, TASK_RUN_STATUS status)
        {
            await Task.Delay(1);
            try
            {

                await Http.PostAsync<FeedbackData, object>($"http://{AGVS.IP}:{AGVS.Port}/api/VmsManager/TaskFeedback", new FeedbackData
                {
                    TimeStamp = DateTime.Now.ToString(),
                    TaskName = runningTaskData.Task_Name,
                    TaskSimplex = runningTaskData.Task_Simplex,
                    TaskSequence = runningTaskData.Task_Sequence,
                    PointIndex = pointIndex,
                    TaskStatus = status,
                });
            }
            catch (Exception ex)
            {
            }

        }

        internal int GetCurrentTagIndexOfTrajectory()
        {
            try
            {
                if (TaskTrackingTags.TryGetValue(ExecutingTask.RunningTaskData.Task_Simplex, out List<int> tags))
                {
                    return tags.IndexOf(tags.First(tag => tag == Navigation.LastVisitedTag));
                }
                else
                {
                    return -1;
                }
            }
            catch (Exception ex)
            {
                LOG.ERROR("GetCurrentTagIndexOfTrajectory exception occur !", ex);
                throw new NullReferenceException();
            }

        }

    }
}
