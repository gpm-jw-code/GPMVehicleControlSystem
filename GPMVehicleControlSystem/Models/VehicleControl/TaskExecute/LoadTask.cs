using AGVSystemCommonNet6.AGVDispatch.Messages;
using AGVSystemCommonNet6.Alarm.VMS_ALARM;
using GPMVehicleControlSystem.Tools;
using static AGVSystemCommonNet6.clsEnums;
using System.Diagnostics;
using AGVSystemCommonNet6.Log;
using GPMVehicleControlSystem.Models.Buzzer;
using System.Formats.Asn1;

namespace GPMVehicleControlSystem.Models.VehicleControl.TaskExecute
{
    /// <summary>
    /// 放貨任務
    /// </summary>
    public class LoadTask : TaskBase
    {
        protected AlarmCodes FrontendSecondarSensorTriggerAlarmCode = AlarmCodes.EQP_LOAD_BUT_EQP_HAS_OBSTACLE;
        public virtual bool CSTTrigger
        {
            get
            {
                return AppSettingsHelper.GetValue<bool>("VCS:CST_READER_TRIGGER");
            }
        }
        public override ACTION_TYPE action { get; set; } = ACTION_TYPE.Load;

        private bool back_to_secondary_flag = false;
        public LoadTask(Vehicle Agv, clsTaskDownloadData taskDownloadData) : base(Agv, taskDownloadData)
        {
        }


        public override async Task<(bool confirm, AlarmCodes alarm_code)> BeforeExecute()
        {
            Agv.FeedbackTaskStatus(TASK_RUN_STATUS.ACTION_START);

            (bool confirm, AlarmCodes alarmCode) CstExistCheckResult = CstExistCheckBeforeHSStartInFrontOfEQ();
            if (!CstExistCheckResult.confirm)
                return (false, CstExistCheckResult.alarmCode);

            (bool confirm, AlarmCodes alarmCode) CstBarcodeCheckResult = await CSTBarcodeReadBeforeAction();

            if (!CstBarcodeCheckResult.confirm)
                return (false, CstBarcodeCheckResult.alarmCode);


            if (RunningTaskData.IsNeedHandshake)
            {
                (bool eqready, AlarmCodes alarmCode) HSResult = await Agv.WaitEQReadyON(action);
                if (!HSResult.eqready)
                {
                    return (false, HSResult.alarmCode);
                }
            }
            StartFrontendObstcleDetection();
            return await base.BeforeExecute();
        }


        public override async Task<(bool confirm, AlarmCodes alarm_code)> AfterMoveDone()
        {
            Agv.DirectionLighter.CloseAll();
            (bool hs_success, AlarmCodes alarmCode) HSResult = await Agv.WaitEQBusyOFF(action);
            if (!HSResult.hs_success)
            {
                Agv.DirectionLighter.CloseAll();
                return (false, HSResult.alarmCode);
            }
            Agv.DirectionLighter.CloseAll();
            //檢查在席
            (bool confirm, AlarmCodes alarmCode) CstExistCheckResult = CstExistCheckAfterEQBusyOff();
            if (!CstExistCheckResult.confirm)
                return (false, CstExistCheckResult.alarmCode);

            back_to_secondary_flag = false;
            await Task.Delay(1000);
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
            HSResult = await Agv.WaitEQReadyOFF(action);
            if (!HSResult.hs_success)
            {
                return (false, HSResult.alarmCode);
            }


            (bool confirm, AlarmCodes alarmCode) CstBarcodeCheckResult = await CSTBarcodeReadAfterAction();
            if (!CstBarcodeCheckResult.confirm)
                return (false, CstBarcodeCheckResult.alarmCode);

            return await base.AfterMoveDone();
        }

        public override void LaserSettingBeforeTaskExecute()
        {
            Agv.Laser.LeftLaserBypass = true;
            Agv.Laser.RightLaserBypass = true;
            Agv.Laser.Mode = VehicleComponent.clsLaser.LASER_MODE.Loading;
        }
        private void AGVC_OnBackTOSecondary(object? sender, clsTaskDownloadData e)
        {
            back_to_secondary_flag = true;
            LOG.INFO($"AGV Back to Secondary Point Done!. Action Finish");
        }

        protected virtual async Task<(bool confirm, AlarmCodes alarmCode)> CSTBarcodeReadBeforeAction()
        {
            if (!CSTTrigger)
                return (true, AlarmCodes.None);
            return await CSTBarcodeRead();
        }

        protected virtual async Task<(bool confirm, AlarmCodes alarmCode)> CSTBarcodeReadAfterAction()
        {
            Agv.CSTReader.ValidCSTID = "";
            //await Agv.AGVC.TriggerCSTReader();
            return (true, AlarmCodes.None);
        }

        protected async Task<(bool confirm, AlarmCodes alarmCode)> CSTBarcodeRead()
        {
            (bool request_success, bool action_done) result = await Agv.AGVC.TriggerCSTReader();
            if (!result.request_success | !result.action_done)
            {
                return (false, AlarmCodes.Read_Cst_ID_Fail);
            }
            if (Agv.CSTReader.Data.data != RunningTaskData.CST.First().CST_ID)
            {
                return (false, AlarmCodes.Cst_ID_Not_Match);
            }
            Agv.CSTReader.ValidCSTID = Agv.CSTReader.Data.data;
            return (true, AlarmCodes.None);
        }

        /// <summary>
        /// 車頭二次檢Sensor檢察功能
        /// </summary>
        protected virtual void StartFrontendObstcleDetection()
        {
            bool Enable = AppSettingsHelper.GetValue<bool>($"VCS:LOAD_OBS_DETECTION:Enable_{action}");

            if (!Enable)
                return;

            int DetectionTime = AppSettingsHelper.GetValue<int>("VCS:LOAD_OBS_DETECTION:Duration");
            LOG.WARN($"前方二次檢Sensor 偵側開始 (偵測持續時間={DetectionTime} s)");
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
                    LOG.Critical($" 前方二次檢Sensor觸發(第 {stopwatch.ElapsedMilliseconds / 1000.0} 秒)");
                    try
                    {
                        Agv.AGVC.EMOHandler(this, EventArgs.Empty);
                        Agv.ExecutingTask.Abort();
                        Agv.Sub_Status = SUB_STATUS.ALARM;
                        AlarmManager.AddAlarm(FrontendSecondarSensorTriggerAlarmCode, false);
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
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
        /// Load作業(放貨)=>車上應該有貨
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        protected virtual (bool confirm, AlarmCodes alarmCode) CstExistCheckBeforeHSStartInFrontOfEQ()
        {
            if (!AppSettingsHelper.GetValue<bool>("VCS:CST_EXIST_DETECTION:Before_In"))
                return (true, AlarmCodes.None);

            if (!Agv.HasAnyCargoOnAGV())
                return (false, AlarmCodes.Has_Job_Without_Cst);

            return (true, AlarmCodes.None);
        }

        /// <summary>
        /// Load完成(放貨)=>車上應該有無貨
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        protected virtual (bool confirm, AlarmCodes alarmCode) CstExistCheckAfterEQBusyOff()
        {
            if (!AppSettingsHelper.GetValue<bool>("VCS:CST_EXIST_DETECTION:After_EQ_Busy_Off"))
                return (true, AlarmCodes.None);

            if (Agv.HasAnyCargoOnAGV())
                return (false, AlarmCodes.Has_Cst_Without_Job);

            Agv.CSTReader.ValidCSTID = "";

            return (true, AlarmCodes.None);
        }

        public override void DirectionLighterSwitchBeforeTaskExecute()
        {
            Agv.DirectionLighter.Forward();
        }

    }
}
