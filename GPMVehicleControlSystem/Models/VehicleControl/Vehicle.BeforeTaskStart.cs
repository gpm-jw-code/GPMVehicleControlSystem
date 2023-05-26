using AGVSystemCommonNet6.AGVDispatch.Messages;
using AGVSystemCommonNet6.Alarm.VMS_ALARM;
using AGVSystemCommonNet6.Log;
using GPMVehicleControlSystem.Models.VehicleControl.AGVControl;
using GPMVehicleControlSystem.Tools;
using GPMVehicleControlSystem.VehicleControl.DIOModule;
using System.Diagnostics;
using static AGVSystemCommonNet6.clsEnums;
using static GPMVehicleControlSystem.Models.VehicleControl.VehicleComponent.clsLaser;

namespace GPMVehicleControlSystem.Models.VehicleControl
{
    public partial class Vehicle
    {

        /// <summary>
        /// 開始移動任務前要做的事
        /// </summary>
        private async Task<(bool confirm, AlarmCodes alarm_code)> ExecuteActionBeforeMoving(clsTaskDownloadData taskDownloadData)
        {

            ACTION_TYPE action = taskDownloadData.Action_Type;

            DirectionLighterSwitchBeforMove(taskDownloadData, action);
            //Laser模式變更
            LaserSettingBeforeMove(action);

            ChargeCircularSwitchBeforeMove(action);

            if (action == ACTION_TYPE.Load | action == ACTION_TYPE.Unload)
            {
                StartFrontendObstcleDetection(action);
                //EQ LDULD需要交握
                if (taskDownloadData.Station_Type == STATION_TYPE.EQ)
                {
                    ///開始與設備交握準備侵入前，檢查在席Sensor
                    (bool confirm, AlarmCodes alarmCode) cst_check_result = CstExistCheckBeforeHSStartInFrontOfEQ(action);

                    if (!cst_check_result.confirm)
                    {
                        return cst_check_result;
                    }
                    //交握
                    (bool eqready, AlarmCodes alarmCode) eqReady_HS_Result = await WaitEQReadyON(action);
                    if (!eqReady_HS_Result.eqready)
                    {
                        return (false, eqReady_HS_Result.alarmCode);
                    }
                    else
                    {
                        LOG.Critical("[EQ Handshake] EQ Ready,AGV 開始侵入");
                    }
                }
            }


            return (true, AlarmCodes.None);

        }

        private void ChargeCircularSwitchBeforeMove(ACTION_TYPE action)
        {
            if (action != ACTION_TYPE.Charge && action != ACTION_TYPE.Discharge)
                return;
            WagoDO.SetState(clsDOModule.DO_ITEM.Recharge_Circuit, action == ACTION_TYPE.Charge);
        }

        private void DirectionLighterSwitchBeforMove(clsTaskDownloadData taskDownloadData, ACTION_TYPE action)
        {
            if (action != ACTION_TYPE.None && action != ACTION_TYPE.Discharge && action != ACTION_TYPE.Escape)
                DirectionLighter.Forward();
            else if (action == ACTION_TYPE.Discharge | taskDownloadData.IsAfterLoadingAction)
                DirectionLighter.Backward();
            else
                DirectionLighter.Forward();
        }

        private void LaserSettingBeforeMove(ACTION_TYPE action)
        {
            if (action == ACTION_TYPE.Charge | action == ACTION_TYPE.Unload | action == ACTION_TYPE.Load | action == ACTION_TYPE.Discharge)
            {
                Laser.Mode = LASER_MODE.Loading;
            }
            else
            {
                Laser.Mode = LASER_MODE.Bypass;
                AGVC.CarSpeedControl(CarController.ROBOT_CONTROL_CMD.SPEED_Reconvery);
            }
        }

        /// <summary>
        /// Load作業(放貨)=>車上應該有貨/ Unload(取貨)=>車上應該無貨
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        private (bool confirm, AlarmCodes alarmCode) CstExistCheckBeforeHSStartInFrontOfEQ(ACTION_TYPE action)
        {
            // "CST_EXIST_DETECTION": {
            //            "Before_In": false,
            //            "After_EQ_Busy_Off": false
            //}
            if (!AppSettingsHelper.GetValue<bool>("VCS:CST_EXIST_DETECTION:Before_In"))
                return (true, AlarmCodes.None);

            if (action == ACTION_TYPE.Load && !HasAnyCargoOnAGV())
            {
                return (false, AlarmCodes.Has_Job_Without_Cst);
            }
            else if (action == ACTION_TYPE.Unload && HasAnyCargoOnAGV())
            {
                return (false, AlarmCodes.Has_Cst_Without_Job);
            }
            return (true, AlarmCodes.None);
        }

        /// <summary>
        /// 車頭二次檢Sensor檢察功能
        /// </summary>
        private void StartFrontendObstcleDetection(ACTION_TYPE action)
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
                        AGVC.EMOHandler(this, EventArgs.Empty);
                        AlarmManager.AddAlarm(AlarmCodes.EQP_LOAD_BUT_EQP_HAS_OBSTACLE);
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                    Sub_Status = SUB_STATUS.DOWN;
                    FeedbackTaskStatus(TASK_RUN_STATUS.ACTION_FINISH);
                    Online_Mode_Switch(REMOTE_MODE.OFFLINE);
                }
            }
            WagoDI.OnFrontSecondObstacleSensorDetected += FrontendObsSensorDetectAction;
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
                WagoDI.OnFrontSecondObstacleSensorDetected -= FrontendObsSensorDetectAction;
            });
        }


    }



}
