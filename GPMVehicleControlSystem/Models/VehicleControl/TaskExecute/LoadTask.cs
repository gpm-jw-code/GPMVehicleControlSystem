using AGVSystemCommonNet6.AGVDispatch.Messages;
using AGVSystemCommonNet6.Alarm.VMS_ALARM;
using GPMVehicleControlSystem.Tools;
using static AGVSystemCommonNet6.clsEnums;
using System.Diagnostics;
using AGVSystemCommonNet6.Log;
using GPMVehicleControlSystem.Models.Buzzer;

namespace GPMVehicleControlSystem.Models.VehicleControl.TaskExecute
{
    /// <summary>
    /// 放貨任務
    /// </summary>
    public class LoadTask : TaskBase
    {

        public override ACTION_TYPE action { get; set; } = ACTION_TYPE.Load;
        private bool back_to_secondary_flag = false;
        public LoadTask(Vehicle Agv, clsTaskDownloadData taskDownloadData) : base(Agv, taskDownloadData)
        {
        }
        public override void LaserSettingBeforeTaskExecute()
        {
            Agv.Laser.LeftLaserBypass = true;
            Agv.Laser.RightLaserBypass = true;
            Agv.Laser.Mode = VehicleComponent.clsLaser.LASER_MODE.Loading;
        }
        public override async Task<(bool confirm, AlarmCodes alarm_code)> AfterMoveDone()
        {
            Agv.DirectionLighter.CloseAll();
            (bool eqready, AlarmCodes alarmCode) HSResult = await Agv.WaitEQBusyOFF(action);
            if (!HSResult.eqready)
            {
                return (false, HSResult.alarmCode);
            }
            Agv.DirectionLighter.CloseAll();
            //檢查在席
            (bool confirm, AlarmCodes alarmCode) CstExistCheckResult = CstExistCheckAfterEQBusyOff(action);
            if (!CstExistCheckResult.confirm)
                return (false, CstExistCheckResult.alarmCode);

            back_to_secondary_flag = false;
            //下Homing Trajectory 任務讓AGV退出
            await Task.Factory.StartNew(async () =>
                       {
                           Agv.DirectionLighter.Backward(delay: 800);
                           RunningTaskData = RunningTaskData.TurnToBackTaskData();
                           Agv.ExecutingTask.RunningTaskData = RunningTaskData;
                           await Agv.AGVC.AGVSTaskDownloadHandler(RunningTaskData);
                           Agv.AGVC.OnTaskActionFinishAndSuccess += AGVC_OnBackTOSecondary;
                       });

            while (!back_to_secondary_flag)
            {
                Thread.Sleep(1);
            }
            Agv.AGVC.OnTaskActionFinishAndSuccess -= AGVC_OnBackTOSecondary;

            return (true, AlarmCodes.None);
        }

        private void AGVC_OnBackTOSecondary(object? sender, clsTaskDownloadData e)
        {
            back_to_secondary_flag = true;
            LOG.INFO($"AGV Back to Secondary Point Done!. Action Finish");
        }

        public override async Task<(bool confirm, AlarmCodes alarm_code)> BeforeExecute()
        {
            Agv.FeedbackTaskStatus(TASK_RUN_STATUS.ACTION_START);

            BuzzerPlayer.BuzzerAction();

            (bool confirm, AlarmCodes alarmCode) CstExistCheckResult = CstExistCheckBeforeHSStartInFrontOfEQ(action);
            if (!CstExistCheckResult.confirm)
                return (false, CstExistCheckResult.alarmCode);

            if (RunningTaskData.IsNeedHandshake)
            {
                (bool eqready, AlarmCodes alarmCode) HSResult = await Agv.WaitEQReadyON(action);
                if (!HSResult.eqready)
                {
                    return (false, HSResult.alarmCode);
                }
            }
            StartFrontendObstcleDetection(action);
            return await base.BeforeExecute();
        }



        /// <summary>
        /// 車頭二次檢Sensor檢察功能
        /// </summary>
        protected void StartFrontendObstcleDetection(ACTION_TYPE action)
        {
            bool Enable = AppSettingsHelper.GetValue<bool>($"VCS:LOAD_OBS_DETECTION:Enable_{action}");
            if (!Enable)
                return;
            int DetectionTime = AppSettingsHelper.GetValue<int>("VCS:LOAD_OBS_DETECTION:Duration");
            LOG.WARN($"前方二次檢Sensor 偵側開始 [{action}](偵測持續時間={DetectionTime} s)");
            CancellationTokenSource cancelDetectCTS = new CancellationTokenSource(TimeSpan.FromSeconds(DetectionTime));
            Stopwatch stopwatch = Stopwatch.StartNew();
            bool detected = false;

            void FrontendObsSensorDetectAction(object sender, EventArgs e)
            {
                detected = true;
                if (!cancelDetectCTS.IsCancellationRequested)
                {
                    cancelDetectCTS.Cancel();
                    stopwatch.Stop();
                    LOG.Critical($"[{action}] 前方二次檢Sensor觸發(第 {stopwatch.ElapsedMilliseconds / 1000.0} 秒)");
                    try
                    {
                        Agv.AGVC.EMOHandler(this, EventArgs.Empty);
                        AlarmManager.AddAlarm(AlarmCodes.EQP_LOAD_BUT_EQP_HAS_OBSTACLE);
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                    Agv.Sub_Status = SUB_STATUS.DOWN;
                    Agv.FeedbackTaskStatus(TASK_RUN_STATUS.ACTION_FINISH);
                    Agv.Online_Mode_Switch(REMOTE_MODE.OFFLINE);
                }
            }
            Agv.WagoDI.OnFrontSecondObstacleSensorDetected += FrontendObsSensorDetectAction;
            Task.Run(() =>
            {
                while (!cancelDetectCTS.IsCancellationRequested)
                {
                    Thread.Sleep(1);
                }
                if (!detected)
                {
                    LOG.WARN($"前方二次檢Sensor Pass. ");
                }
                Agv.WagoDI.OnFrontSecondObstacleSensorDetected -= FrontendObsSensorDetectAction;
            });
        }


        /// <summary>
        /// Load作業(放貨)=>車上應該有貨/ Unload(取貨)=>車上應該無貨
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        private (bool confirm, AlarmCodes alarmCode) CstExistCheckBeforeHSStartInFrontOfEQ(ACTION_TYPE action)
        {
            if (Debugger.IsAttached)
                return (true, AlarmCodes.None);
            // "CST_EXIST_DETECTION": {
            //            "Before_In": false,
            //            "After_EQ_Busy_Off": false
            //}
            if (!AppSettingsHelper.GetValue<bool>("VCS:CST_EXIST_DETECTION:Before_In"))
                return (true, AlarmCodes.None);

            if (action == ACTION_TYPE.Load && !Agv.HasAnyCargoOnAGV())
            {
                return (false, AlarmCodes.Has_Job_Without_Cst);
            }
            else if (action == ACTION_TYPE.Unload && Agv.HasAnyCargoOnAGV())
            {
                return (false, AlarmCodes.Has_Cst_Without_Job);
            }
            return (true, AlarmCodes.None);
        }

        /// <summary>
        /// Load完成(放貨)=>車上應該有無貨/ Unload完成(取貨)=>車上應該有貨
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        private (bool confirm, AlarmCodes alarmCode) CstExistCheckAfterEQBusyOff(ACTION_TYPE action)
        {

            if (Debugger.IsAttached)
                return (true, AlarmCodes.None);
            // "CST_EXIST_DETECTION": {
            //            "Before_In": false,
            //            "After_EQ_Busy_Off": false
            //}
            if (!AppSettingsHelper.GetValue<bool>("VCS:CST_EXIST_DETECTION:After_EQ_Busy_Off"))
                return (true, AlarmCodes.None);


            if (action == ACTION_TYPE.Load && Agv.HasAnyCargoOnAGV())
            {
                return (false, AlarmCodes.Has_Cst_Without_Job);
            }
            else if (action == ACTION_TYPE.Unload && !Agv.HasAnyCargoOnAGV())
            {
                return (false, AlarmCodes.Has_Job_Without_Cst);
            }
            return (true, AlarmCodes.None);
        }

    }
}
