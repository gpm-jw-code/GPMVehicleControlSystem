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
        private clsTaskDownloadData _RunningTaskData;
        public Action<string> OnTaskFinish;
        public clsTaskDownloadData RunningTaskData
        {
            get => _RunningTaskData;
            set
            {

                if (_RunningTaskData == null | value.Task_Name != _RunningTaskData?.Task_Name)
                {
                    TrackingTags = value.TagsOfTrajectory;
                }
                else
                {
                    List<int> newTrackingTags = value.TagsOfTrajectory;

                    if (TrackingTags.First() != newTrackingTags.First())
                    {
                        //1 2 3 
                        //5
                    }
                    else
                    {
                        TrackingTags = newTrackingTags;

                    }
                }
                _RunningTaskData = value;
            }
        }
        public abstract ACTION_TYPE action { get; set; }
        public List<int> TrackingTags { get; private set; } = new List<int>();
        public TaskBase(Vehicle Agv, clsTaskDownloadData taskDownloadData)
        {
            this.Agv = Agv;
            RunningTaskData = taskDownloadData;

            LOG.INFO($"New Task : \r\nTask Name:{taskDownloadData.Task_Name}\r\n Task_Simplex:{taskDownloadData.Task_Simplex}\r\nTask_Sequence:{taskDownloadData.Task_Sequence}");

        }

        /// <summary>
        /// 執行任務
        /// </summary>
        public async Task Execute()
        {
            Agv.AGVC.IsAGVExecutingTask = true;
            Agv.AGVC.OnTaskActionFinishAndSuccess += AfterMoveFinishHandler;

            (bool confirm, AlarmCodes alarm_code) checkResult = await BeforeExecute();

            if (!checkResult.confirm)
            {
                AlarmManager.AddAlarm(checkResult.alarm_code);
                Agv.Sub_Status = SUB_STATUS.ALARM;
            }
            bool agvc_executing = await Agv.AGVC.AGVSTaskDownloadHandler(RunningTaskData);
            if (!agvc_executing)
            {
                AlarmManager.AddAlarm(AlarmCodes.Cant_TransferTask_TO_AGVC);
                Agv.Sub_Status = SUB_STATUS.ALARM;
            }
        }

        internal async Task AGVSPathExpand(clsTaskDownloadData taskDownloadData)
        {
            string new_path = string.Join("->", taskDownloadData.TagsOfTrajectory);
            Agv.AGVC.Replan(taskDownloadData);
            string ori_path = string.Join("->", RunningTaskData.TagsOfTrajectory);
            LOG.INFO($"AGV導航路徑變更\r\n-原路徑：{ori_path}\r\n新路徑:{new_path}");
            RunningTaskData = taskDownloadData;
        }

        private async void AfterMoveFinishHandler(object? sender, clsTaskDownloadData e)
        {

            LOG.INFO($" [{action}] move task done. Reach  Tag = {Agv.Navigation.LastVisitedTag} ");

            Agv.AGVC.OnTaskActionFinishAndSuccess -= AfterMoveFinishHandler;
            (bool confirm, AlarmCodes alarm_code) check_result = await AfterMoveDone();
            if (!check_result.confirm)
            {
                AlarmManager.AddAlarm(check_result.alarm_code);
            }

            Agv.Sub_Status = SUB_STATUS.IDLE;
            Agv.AGVC.IsAGVExecutingTask = false;
            OnTaskFinish(RunningTaskData.Task_Simplex);
        }

        public virtual async Task<(bool confirm, AlarmCodes alarm_code)> AfterMoveDone()
        {
            Agv.Laser.LeftLaserBypass = false;
            Agv.Laser.RightLaserBypass = false;
            Agv.Sub_Status = SUB_STATUS.IDLE;
            Agv.FeedbackTaskStatus(TASK_RUN_STATUS.ACTION_FINISH);
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

        internal void Abort()
        {
            Agv.AGVC.OnTaskActionFinishAndSuccess -= AfterMoveFinishHandler;
        }
    }
}
