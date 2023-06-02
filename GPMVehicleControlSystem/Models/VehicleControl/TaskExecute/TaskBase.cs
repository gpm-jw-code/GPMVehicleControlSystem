using AGVSystemCommonNet6.AGVDispatch.Messages;
using AGVSystemCommonNet6.Alarm.VMS_ALARM;
using AGVSystemCommonNet6.Log;
using RosSharp.RosBridgeClient.Actionlib;
using static AGVSystemCommonNet6.clsEnums;

namespace GPMVehicleControlSystem.Models.VehicleControl.TaskExecute
{
    public abstract class TaskBase
    {
        public Vehicle Agv { get; }
        public clsTaskDownloadData RunningTaskData { get; set; } = new clsTaskDownloadData();
        public abstract ACTION_TYPE action { get; set; }

        public TaskBase(Vehicle Agv, clsTaskDownloadData taskDownloadData)
        {
            this.Agv = Agv;
            RunningTaskData = taskDownloadData;
        }

        /// <summary>
        /// 執行任務
        /// </summary>
        public async Task Execute()
        {
            Agv.AGVC.IsAGVExecutingTask = true;
            (bool confirm, AlarmCodes alarm_code) checkResult = await BeforeExecute();
            if (!checkResult.confirm)
            {
                AlarmManager.AddAlarm(checkResult.alarm_code);
                Agv.Sub_Status = SUB_STATUS.ALARM;
            }
            Agv.AGVC.OnTaskActionFinishAndSuccess += AfterMoveFinish;
            bool agvc_executing = await Agv.AGVC.AGVSTaskDownloadHandler(RunningTaskData);
            if (!agvc_executing)
            {
                AlarmManager.AddAlarm(AlarmCodes.Cant_TransferTask_TO_AGVC);
                Agv.Sub_Status = SUB_STATUS.ALARM;
            }
        }

        private async void AfterMoveFinish(object? sender, clsTaskDownloadData e)
        {

            LOG.INFO($" [{action}] move task done. Reach  Tag = {Agv.Navigation.LastVisitedTag} ");

            Agv.AGVC.OnTaskActionFinishAndSuccess -= AfterMoveFinish;

            (bool confirm, AlarmCodes alarm_code) check_result = await AfterMoveDone();
            if (!check_result.confirm)
            {
                AlarmManager.AddAlarm(check_result.alarm_code);
            }
        }

        public virtual async Task<(bool confirm, AlarmCodes alarm_code)> AfterMoveDone()
        {
            Agv.Sub_Status = SUB_STATUS.IDLE;
            Agv.FeedbackTaskStatus(TASK_RUN_STATUS.ACTION_FINISH);
            Agv.AGVC.IsAGVExecutingTask = false;
            return (true, AlarmCodes.None);

        }

        /// <summary>
        /// 執行任務前的各項設定
        /// </summary>
        /// <returns></returns>
        public virtual async Task<(bool confirm, AlarmCodes alarm_code)> BeforeExecute()
        {
            DirectionLighterSwitchBeforeTaskExecute();
            LaserSettingBeforeTaskExecute();

            return (true, AlarmCodes.None);
        }

        /// <summary>
        /// 任務開始前的方向燈切換
        /// </summary>
        public virtual void DirectionLighterSwitchBeforeTaskExecute()
        {
            Agv.DirectionLighter.Forward();
        }

        /// <summary>
        /// 任務開始前的雷射設定
        /// </summary>
        public virtual async void LaserSettingBeforeTaskExecute()
        {
            await Agv.AGVC.CarSpeedControl(AGVControl.CarController.ROBOT_CONTROL_CMD.SPEED_Reconvery);
        }

        internal async Task AGVSPathExpand(clsTaskDownloadData taskDownloadData)
        {
            RunningTaskData = taskDownloadData;
            string new_path = string.Join("->", taskDownloadData.TagsOfTrajectory);
            if (RunningTaskData.Task_Name != taskDownloadData.Task_Name)
            {
                throw new Exception("任務ID不同");
            }
            Agv.AGVC.Replan(taskDownloadData);


            string ori_path = string.Join("->", RunningTaskData.TagsOfTrajectory);
            LOG.TRACE($"AGV導航路徑變更\r\n-原路徑：{ori_path}\r\n新路徑:{new_path}");
        }

        internal void Abort()
        {
            Agv.AGVC.OnTaskActionFinishAndSuccess -= AfterMoveFinish;
        }
    }
}
