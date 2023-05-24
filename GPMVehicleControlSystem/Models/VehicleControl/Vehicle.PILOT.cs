using AGVSystemCommonNet6.AGVDispatch.Messages;
using AGVSystemCommonNet6.Alarm.VMS_ALARM;
using AGVSystemCommonNet6.GPMRosMessageNet.Messages;
using AGVSystemCommonNet6.Log;
using GPMVehicleControlSystem.Models.VehicleControl.AGVControl;
using GPMVehicleControlSystem.Tools;
using GPMVehicleControlSystem.VehicleControl.DIOModule;
using System.Diagnostics;
using static AGVSystemCommonNet6.clsEnums;
using static GPMVehicleControlSystem.Models.VehicleControl.Vehicle;
using static GPMVehicleControlSystem.Models.VehicleControl.VehicleComponent.clsLaser;

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
        /// <summary>
        /// 開始移動任務前要做的事
        /// </summary>
        private async Task<(bool confirm, AlarmCodes alarm_code)> ExecuteActionBeforeMoving(clsTaskDownloadData taskDownloadData)
        {

            ACTION_TYPE action = taskDownloadData.Action_Type;

            if (action != ACTION_TYPE.None && action != ACTION_TYPE.Discharge && action != ACTION_TYPE.Escape)
                DirectionLighter.Forward();
            else if (action == ACTION_TYPE.Discharge | taskDownloadData.IsAfterLoadingAction)
                DirectionLighter.Backward();
            else
                DirectionLighter.Forward();
            //Laser模式變更

            if (action == ACTION_TYPE.Charge | action == ACTION_TYPE.Unload | action == ACTION_TYPE.Load | action == ACTION_TYPE.Discharge)
            {
                Laser.Mode = LASER_MODE.Loading;
            }
            else
            {
                Laser.Mode = LASER_MODE.Bypass;
                AGVC.CarSpeedControl(CarController.ROBOT_CONTROL_CMD.SPEED_Reconvery);
            }


            if (action == ACTION_TYPE.Charge)
            {
                WagoDO.SetState(clsDOModule.DO_ITEM.Recharge_Circuit, true);
            }
            else if (action == ACTION_TYPE.Discharge)
            {
                WagoDO.SetState(clsDOModule.DO_ITEM.Recharge_Circuit, false);
            }
            else if (action == ACTION_TYPE.Load)
            {
                //TODO 在席檢查開關
                ////檢查在席全ON(車上應該要有貨)
                //if (!HasAnyCargoOnAGV())
                //{
                //    return (false, AlarmCodes.Has_Job_Without_Cst);
                //}


                bool Enable = AppSettingsHelper.GetValue<bool>("VCS:LOAD_OBS_DETECTION:Enable_Load");
                if (Enable)
                    StartFrontendObstcleDetection("ACTION_TYPE.Load");
            }
            else if (action == ACTION_TYPE.Unload)
            {
                //TODO 在席檢查開關
                ////檢查在席全ON(車上應該要沒貨)
                //if (HasAnyCargoOnAGV())
                //{
                //    return (false, AlarmCodes.Has_Cst_Without_Job);
                //}


                bool Enable = AppSettingsHelper.GetValue<bool>("VCS:LOAD_OBS_DETECTION:Enable_Unload");
                if (Enable)
                    StartFrontendObstcleDetection("ACTION_TYPE.Unload");
            }

            //EQ LDULD需要交握
            if (taskDownloadData.Station_Type == STATION_TYPE.EQ)
            {
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

            return (true, AlarmCodes.None);

        }
        /// <summary>
        /// 移動任務結束後的處理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="taskData"></param>
        private async void AGVMoveTaskActionSuccessHandle(object? sender, clsTaskDownloadData taskData)
        {
            if (AGV_Reset_Flag)
                return;

            Sub_Status = SUB_STATUS.IDLE;
            //AGVC.CarSpeedControl(CarController.ROBOT_CONTROL_CMD.STOP_WHEN_REACH_GOAL);
            await Task.Delay(500);
            try
            {
                bool isActionFinish = Navigation.Data.lastVisitedNode.data == taskData.Destination;
                if (Main_Status != MAIN_STATUS.IDLE)
                {
                    throw new Exception("ACTION FINISH Feedback But AGV MAIN STATUS is not IDLE");
                }
                if (taskData.Action_Type != ACTION_TYPE.None)
                {
                    CurrentTaskRunStatus = TASK_RUN_STATUS.NAVIGATING;
                    if (taskData.IsTaskSegmented)
                    {
                        //侵入Port後

                        await FeedbackTaskStatus(CurrentTaskRunStatus);
                        var check_result_after_Task = await ExecuteWorksWhenReachPort(taskData);

                        if (!check_result_after_Task.confirm)
                        {
                            AlarmManager.AddAlarm(check_result_after_Task.alarm_code);
                            Sub_Status = SUB_STATUS.DOWN;
                            return;
                        }
                    }
                    else
                    {
                        /// 退出Port後
                        if (taskData.Station_Type == STATION_TYPE.EQ)
                        {
                            (bool eqready_off, AlarmCodes alarmCode) result = await WaitEQReadyOFF(taskData.Action_Type);
                            if (!result.eqready_off)
                            {
                                AlarmManager.AddAlarm(result.alarmCode);
                                Sub_Status = SUB_STATUS.DOWN;
                            }
                            else
                            {
                                LOG.Critical("[EQ Handshake] HADNSHAKE NORMAL Done,AGV Next TASK Will START");
                            }
                        }
                        await FeedbackTaskStatus(TASK_RUN_STATUS.ACTION_FINISH);

                    }
                }
                else
                {
                    await FeedbackTaskStatus(TASK_RUN_STATUS.ACTION_FINISH);
                }

            }
            catch (Exception ex)
            {
                LOG.ERROR("AGVMoveTaskActionSuccessHandle", ex);
            }
            await Task.Delay(500);
            DirectionLighter.CloseAll();
        }

        /// <summary>
        /// 移動任務結束後
        /// </summary>
        /// <param name="taskDownloadData"></param>
        private async Task<(bool confirm, AlarmCodes alarm_code)> ExecuteWorksWhenReachPort(clsTaskDownloadData taskDownloadData)
        {
            ACTION_TYPE action = taskDownloadData.Action_Type;

            if (action == ACTION_TYPE.Load | action == ACTION_TYPE.Unload)
            {

                if (taskDownloadData.Station_Type == STATION_TYPE.EQ)
                {
                    //交握
                    var eqBusy_OFF_HS_Result = await WaitEQBusyOFF(action);
                    if (!eqBusy_OFF_HS_Result.eq_busy_off)
                    {
                        return (false, eqBusy_OFF_HS_Result.alarmCode);
                    }
                    else
                        LOG.Critical("[EQ Handshake] EQ BUSY OFF,AGV 開始退出EQ");
                    DirectionLighter.AbortFlash();
                }
                //TODO 在席檢查開關
                //if (action == ACTION_TYPE.Load)
                //{
                //    //檢查在席全ON(車上應該要沒貨)
                //    if (HasAnyCargoOnAGV())
                //    {
                //        return (false, AlarmCodes.Has_Cst_Without_Job);
                //    }
                //}

                //if (action == ACTION_TYPE.Unload)
                //{
                //    //檢查在席全ON(車上應該要沒貨)
                //    if (!HasAnyCargoOnAGV())
                //    {
                //        return (false, AlarmCodes.Has_Job_Without_Cst);
                //    }
                //}

                clsTaskDownloadData _AGVBackTaskDownloadData = taskDownloadData.TurnToBackTaskData();
                if (!await AGVC.AGVSTaskDownloadHandler(_AGVBackTaskDownloadData))
                {
                    return (false, AlarmCodes.Cant_TransferTask_TO_AGVC);

                }
            }
            return (true, AlarmCodes.None);

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

        /// <summary>
        /// 車頭二次檢Sensor檢察功能
        /// </summary>
        private void StartFrontendObstcleDetection(string action = "")
        {
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
                    LOG.Critical($"前方二次檢Sensor觸發(第 {stopwatch.ElapsedMilliseconds / 1000.0} 秒)");
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
