using AGVSystemCommonNet6.AGVDispatch.Messages;
using AGVSystemCommonNet6.Alarm.VMS_ALARM;
using AGVSystemCommonNet6.GPMRosMessageNet.Messages;
using AGVSystemCommonNet6.Log;
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
        public clsTaskDownloadData RunningTaskData => AGVC.RunningTaskData;

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


        /// <summary>
        /// 執行派車系統任務
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="taskDownloadData"></param>
        internal void ExecuteAGVSTask(object? sender, clsTaskDownloadData taskDownloadData)
        {
            LOG.INFO($"Task Download: Task Name = {taskDownloadData.Task_Name} , Task Simple = {taskDownloadData.Task_Simplex}");

            Task.Run(async () =>
            {
                var check_result_before_Task = await ExecuteActionBeforeMoving(taskDownloadData);

                if (!check_result_before_Task.confirm)
                {
                    AlarmManager.AddAlarm(check_result_before_Task.alarm_code);
                    Sub_Status = SUB_STATUS.DOWN;
                    return;
                }

                await Task.Delay(1000);
                if (AGVC.IsAGVExecutingTask)
                {
                    LOG.Critical($"在 TAG {BarcodeReader.CurrentTag} 收到新的路徑擴充任務");
                    await AGVC.AGVSPathExpand(taskDownloadData);
                }
                else
                {

                    bool agv_running = await AGVC.AGVSTaskDownloadHandler(taskDownloadData);
                    if (agv_running)
                    {
                        Sub_Status = SUB_STATUS.RUN;
                    }
                    else
                    {
                        LOG.Critical($"無法發送任務給車控執行");
                        Sub_Status = SUB_STATUS.DOWN;
                    }
                }
            });
        }


        /// <summary>
        /// 當車子開始執行任務的時候
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="taskData"></param>
        private async void CarController_OnMoveTaskStart(object? sender, clsTaskDownloadData taskData)
        {
            Sub_Status = SUB_STATUS.RUN;

            if (taskData.Action_Type == ACTION_TYPE.None)
            {
                await FeedbackTaskStatus(TASK_RUN_STATUS.NAVIGATING);
            }
            else
            {
                await FeedbackTaskStatus(TASK_RUN_STATUS.ACTION_START);
            }

        }


        private void OnTagLeaveHandler(object? sender, int leaveTag)
        {
            if (Operation_Mode == OPERATOR_MODE.MANUAL)
                return;

            if (RunningTaskData.Action_Type == ACTION_TYPE.None)
                Laser.ApplyAGVSLaserSetting();

        }
        private void OnTagReachHandler(object? sender, int currentTag)
        {
            if (Operation_Mode == OPERATOR_MODE.MANUAL)
                return;

            var TagPoint = RunningTaskData.ExecutingTrajecory.FirstOrDefault(pt => pt.Point_ID == currentTag);
            if (TagPoint == null)
            {
                LOG.Critical($"AGV抵達 {currentTag} 但在任務軌跡上找不到該站點。");
                return;
            }
            PathInfo? pathInfoRos = RunningTaskData.RosTaskCommandGoal?.pathInfo.FirstOrDefault(path => path.tagid == TagPoint.Point_ID);
            if (pathInfoRos == null)
            {
                AGVC.AbortTask();
                AlarmManager.AddAlarm(AlarmCodes.Motion_control_Tracking_Tag_Not_On_Tag_Or_Tap, true);
                Sub_Status = SUB_STATUS.DOWN;
                return;
            }
            Laser.AgvsLsrSetting = TagPoint.Laser;

            LOG.INFO($"AGV抵達 Tag {currentTag},派車雷射設定:{Laser.AgvsLsrSetting}");
        }

       

        private void CarController_OnTaskActionFinishCauseAbort(object? sender, clsTaskDownloadData e)
        {
        }


        private async void AGVC_OnTaskActionFinishButNeedToExpandPath(object? sender, clsTaskDownloadData taskData)
        {
            await Task.Delay(200);
            LOG.INFO($"Task Feedback when Action done but need to expand path");

            await FeedbackTaskStatus(TASK_RUN_STATUS.NAVIGATING);

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
            return true;
        }

        private async Task FeedbackTaskStatus(TASK_RUN_STATUS status)
        {
            await Task.Delay(1);
            if (Remote_Mode == REMOTE_MODE.OFFLINE)
                return;
            await AGVS.TryTaskFeedBackAsync(AGVC.RunningTaskData, GetCurrentTagIndexOfTrajectory(), status);
        }
        internal int GetCurrentTagIndexOfTrajectory()
        {
            try
            {
                return RunningTaskData.ExecutingTrajecory.ToList().IndexOf(RunningTaskData.ExecutingTrajecory.First(pt => pt.Point_ID == BarcodeReader.CurrentTag));

            }
            catch (Exception)
            {
                return 0;
            }

        }

    }
}
