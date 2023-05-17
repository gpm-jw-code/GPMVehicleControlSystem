using GPMVehicleControlSystem.Models.AGVDispatch.Messages;
using GPMVehicleControlSystem.Models.Alarm;
using GPMVehicleControlSystem.Models.VehicleControl.AGVControl;
using GPMVehicleControlSystem.Tools;
using System.Diagnostics;
using static GPMVehicleControlSystem.Models.VehicleControl.Vehicle;

namespace GPMVehicleControlSystem.Models.VehicleControl
{
    /// <summary>
    /// AGV駕駛員,可以控制車子(包含元件跟車控)以及跟AGVS溝通
    /// </summary>
    public class AGVPILOT
    {

        public enum HS_METHOD
        {
            E84, MODBUS, EMULATION
        }

        /// <summary>
        /// 車子
        /// </summary>
        public Vehicle AGV { get; }
        /// <summary>
        /// 車控
        /// </summary>
        private CarController AGVC => AGV.CarController;

        /// <summary>
        /// 派車
        /// </summary>
        private AGVDispatch.clsAGVSConnection AGVS => AGV.AGVSConnection;
        public HS_METHOD Hs_Method = HS_METHOD.EMULATION;
        public AGVPILOT(Vehicle AGV)
        {
            this.AGV = AGV;
            AGVC.OnTaskActionFinishAndSuccess += AGVMoveTaskActionSuccessHandle;
            AGVC.OnTaskActionFinishCauseAbort += CarController_OnTaskActionFinishCauseAbort;
            AGVC.OnTaskActionFinishButNeedToExpandPath += AGVC_OnTaskActionFinishButNeedToExpandPath; ;
            AGVC.OnMoveTaskStart += CarController_OnMoveTaskStart;
            AGVS.OnTaskDownload += AGVSTaskDownloadConfirm;
            AGVS.OnTaskResetReq = AGVSTaskResetReqHandle;
            AGVS.OnTaskDownloadFeekbackDone += ExecuteAGVSTask;

        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="taskDownloadData"></param>
        /// <returns></returns>
        internal bool AGVSTaskDownloadConfirm(clsTaskDownloadData taskDownloadData)
        {
            AGV.AGV_Reset_Flag = false;

            if (AGV.Sub_Status != SUB_STATUS.IDLE) //TODO More Status Confirm when recieve AGVS Task
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
                await Task.Delay(100);
                var check_result_before_Task = await ExecuteActionBeforeMoving(taskDownloadData);

                if (!check_result_before_Task.confirm)
                {
                    AlarmManager.AddAlarm(check_result_before_Task.alarm_code);
                    AGV.Sub_Status = SUB_STATUS.DOWN;
                    return;
                }
                if (AGVC.IsAGVExecutingTask)
                {
                    LOG.Critical($"在 TAG {AGV.BarcodeReader.CurrentTag} 收到新的路徑擴充任務");
                    await AGVC.AGVSPathExpand(taskDownloadData);
                }
                else
                {

                    bool agv_running = await AGVC.AGVSTaskDownloadHandler(taskDownloadData);
                    if (agv_running)
                    {
                        AGV.Sub_Status = SUB_STATUS.RUN;
                    }
                    else
                    {
                        LOG.Critical($"無法發送任務給車控執行");
                        AGV.Sub_Status = SUB_STATUS.DOWN;
                    }
                }
            });
        }

        /// <summary>
        /// 開始移動任務前要做的事
        /// </summary>
        private async Task<(bool confirm, AlarmCodes alarm_code)> ExecuteActionBeforeMoving(clsTaskDownloadData taskDownloadData)
        {
            ACTION_TYPE action = taskDownloadData.Action_Type;

            if (action != ACTION_TYPE.None && action != ACTION_TYPE.Discharge && action != ACTION_TYPE.Escape)
            {
                AGV.DirectionLighter.Forward();
            }
            else
                AGV.DirectionLighter.Backward();


            if (action == ACTION_TYPE.Charge)
            {
                AGV.WagoDO.SetState(DIOModule.clsDOModule.DO_ITEM.Recharge_Circuit, true);
            }
            else if (action == ACTION_TYPE.Discharge)
            {
                AGV.WagoDO.SetState(DIOModule.clsDOModule.DO_ITEM.Recharge_Circuit, false);
            }
            else if (action == ACTION_TYPE.Load)
            {
                //TODO 在席檢查開關
                ////檢查在席全ON(車上應該要有貨)
                //if (!AGV.HasAnyCargoOnAGV())
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
                //if (AGV.HasAnyCargoOnAGV())
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
        /// 移動任務結束後
        /// </summary>
        /// <param name="taskDownloadData"></param>
        private async Task<(bool confirm, AlarmCodes alarm_code)> ExecuteActionAfterMoving(clsTaskDownloadData taskDownloadData)
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
                    AGV.DirectionLighter.AbortFlash();
                }
                //TODO 在席檢查開關
                //if (action == ACTION_TYPE.Load)
                //{
                //    //檢查在席全ON(車上應該要沒貨)
                //    if (AGV.HasAnyCargoOnAGV())
                //    {
                //        return (false, AlarmCodes.Has_Cst_Without_Job);
                //    }
                //}

                //if (action == ACTION_TYPE.Unload)
                //{
                //    //檢查在席全ON(車上應該要沒貨)
                //    if (!AGV.HasAnyCargoOnAGV())
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
            void FrontendObsSensorDetectAction()
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
                    AGV.Sub_Status = SUB_STATUS.DOWN;
                    AGVS.TryTaskFeedBackAsync(AGVC.RunningTaskData, AGVC.GetCurrentTagIndexOfTrajectory(AGV.BarcodeReader.CurrentTag), TASK_RUN_STATUS.ACTION_FINISH);
                    AGV.Online_Mode_Switch(REMOTE_MODE.OFFLINE);
                }
            }
            AGV.WagoDI.OnFrontSecondObstacleSensorDetected += FrontendObsSensorDetectAction;
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
                AGV.WagoDI.OnFrontSecondObstacleSensorDetected -= FrontendObsSensorDetectAction;
            });
        }


        private async void CarController_OnMoveTaskStart(object? sender, clsTaskDownloadData taskData)
        {
            AGV.Sub_Status = SUB_STATUS.RUN;

            if (taskData.Action_Type == ACTION_TYPE.None)
            {

                await AGVS.TryTaskFeedBackAsync(taskData, AGVC.GetCurrentTagIndexOfTrajectory(AGV.BarcodeReader.CurrentTag), TASK_RUN_STATUS.NAVIGATING);
            }
            else
            {
                await AGVS.TryTaskFeedBackAsync(taskData, AGVC.GetCurrentTagIndexOfTrajectory(AGV.BarcodeReader.CurrentTag), TASK_RUN_STATUS.ACTION_START);


            }

        }

        private void CarController_OnTaskActionFinishCauseAbort(object? sender, clsTaskDownloadData e)
        {
        }


        /// <summary>
        /// 移動任務結束後的處理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="taskData"></param>
        private async void AGVMoveTaskActionSuccessHandle(object? sender, clsTaskDownloadData taskData)
        {
            AGV.Sub_Status = SUB_STATUS.IDLE;
            //AGVC.CarSpeedControl(CarController.ROBOT_CONTROL_CMD.STOP_WHEN_REACH_GOAL);
            await Task.Delay(500);
            try
            {
                bool isActionFinish = AGV.Navigation.Data.lastVisitedNode.data == taskData.Destination;
                if (AGV.Main_Status != MAIN_STATUS.IDLE)
                {
                    throw new Exception("ACTION FINISH Feedback But AGV MAIN STATUS is not IDLE");
                }

                if (taskData.Action_Type != ACTION_TYPE.None)
                {
                    if (!taskData.IsAfterLoadingAction)
                    {
                        await AGVS.TryTaskFeedBackAsync(taskData, AGVC.GetCurrentTagIndexOfTrajectory(AGV.BarcodeReader.CurrentTag), TASK_RUN_STATUS.NAVIGATING);
                        var check_result_after_Task = await ExecuteActionAfterMoving(taskData);

                        if (!check_result_after_Task.confirm)
                        {
                            AlarmManager.AddAlarm(check_result_after_Task.alarm_code);
                            AGV.Sub_Status = SUB_STATUS.DOWN;
                            return;
                        }
                    }
                    else
                    {
                        if (taskData.Station_Type == STATION_TYPE.EQ)
                        {
                            (bool eqready_off, AlarmCodes alarmCode) result = await WaitEQReadyOFF(taskData.Action_Type);
                            if (!result.eqready_off)
                            {
                                AlarmManager.AddAlarm(result.alarmCode);
                                AGV.Sub_Status = SUB_STATUS.DOWN;
                            }
                            else
                            {
                                LOG.Critical("[EQ Handshake] HADNSHAKE NORMAL Done,AGV Next TASK Will START");
                            }
                        }
                        await AGVS.TryTaskFeedBackAsync(taskData, AGVC.GetCurrentTagIndexOfTrajectory(AGV.BarcodeReader.CurrentTag), TASK_RUN_STATUS.ACTION_FINISH);

                    }
                }
                else
                    await AGVS.TryTaskFeedBackAsync(taskData, AGVC.GetCurrentTagIndexOfTrajectory(AGV.BarcodeReader.CurrentTag), TASK_RUN_STATUS.ACTION_FINISH);
            }
            catch (Exception ex)
            {
                LOG.ERROR("AGVMoveTaskActionSuccessHandle", ex);
            }
            await Task.Delay(500);
            AGV.DirectionLighter.CloseAll();
        }
        private async void AGVC_OnTaskActionFinishButNeedToExpandPath(object? sender, clsTaskDownloadData taskData)
        {
            await Task.Delay(200);
            LOG.INFO($"Task Feedback when Action done but need to expand path");
            await AGVS.TryTaskFeedBackAsync(taskData, AGVC.GetCurrentTagIndexOfTrajectory(AGV.BarcodeReader.CurrentTag), TASK_RUN_STATUS.NAVIGATING);

        }

        #region 交握

        /// <summary>
        /// 開始與EQ進行交握_等待EQ READY
        /// </summary>
        private async Task<(bool eqready, AlarmCodes alarmCode)> WaitEQReadyON(ACTION_TYPE action)
        {
            CancellationTokenSource waitEQSignalCST = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            Task wait_eq_UL_req_ON = new Task(() =>
            {
                while (action == ACTION_TYPE.Load ? !AGV.WagoDI.GetState(DIOModule.clsDIModule.DI_ITEM.EQ_L_REQ) : !AGV.WagoDI.GetState(DIOModule.clsDIModule.DI_ITEM.EQ_U_REQ))
                {
                    if (waitEQSignalCST.IsCancellationRequested)
                        return;
                    Thread.Sleep(1);
                }
            });
            Task wait_eq_ready = new Task(() =>
            {
                while (!AGV.WagoDI.GetState(DIOModule.clsDIModule.DI_ITEM.EQ_READY))
                {
                    if (waitEQSignalCST.IsCancellationRequested)
                        return;
                    Thread.Sleep(1);
                }
            });

            AGV.WagoDO.SetState(DIOModule.clsDOModule.DO_ITEM.AGV_VALID, true);

            if (Hs_Method == HS_METHOD.EMULATION)
            {
                AGV.WagoDO.SetState(DIOModule.clsDOModule.DO_ITEM.AGV_TR_REQ, true);
                AGV.WagoDO.SetState(DIOModule.clsDOModule.DO_ITEM.AGV_BUSY, true);
                return (true, AlarmCodes.None);
            }

            waitEQSignalCST = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            try
            {
                wait_eq_UL_req_ON.Start();
                wait_eq_UL_req_ON.Wait(waitEQSignalCST.Token);
            }
            catch (OperationCanceledException ex)
            {
                return (false, action == ACTION_TYPE.Load ? AlarmCodes.Handshake_Fail_EQ_L_REQ : AlarmCodes.Handshake_Fail_EQ_U_REQ);

            }

            AGV.WagoDO.SetState(DIOModule.clsDOModule.DO_ITEM.AGV_TR_REQ, true);
            try
            {
                wait_eq_ready.Start();
                wait_eq_ready.Wait(waitEQSignalCST.Token);
                AGV.WagoDO.SetState(DIOModule.clsDOModule.DO_ITEM.AGV_BUSY, true);
                return (true, AlarmCodes.None);
            }
            catch (OperationCanceledException ex)
            {
                return (false, AlarmCodes.Handshake_Fail_EQ_READY);

            }

        }

        /// <summary>
        /// 開始與EQ進行交握_等待EQ READY OFF
        /// </summary>
        private async Task<(bool eqready_off, AlarmCodes alarmCode)> WaitEQReadyOFF(ACTION_TYPE action)
        {
            LOG.Critical("[EQ Handshake] 等待EQ READY OFF");
            CancellationTokenSource waitEQSignalCST = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            Task wait_eq_UL_req_OFF = new Task(() =>
            {
                while (action == ACTION_TYPE.Load ? AGV.WagoDI.GetState(DIOModule.clsDIModule.DI_ITEM.EQ_L_REQ) : AGV.WagoDI.GetState(DIOModule.clsDIModule.DI_ITEM.EQ_U_REQ))
                {
                    if (waitEQSignalCST.IsCancellationRequested)
                        return;
                    Thread.Sleep(1);
                }
            });
            Task wait_eq_ready_off = new Task(() =>
            {
                while (AGV.WagoDI.GetState(DIOModule.clsDIModule.DI_ITEM.EQ_READY))
                {
                    if (waitEQSignalCST.IsCancellationRequested)
                        return;
                    Thread.Sleep(1);
                }
            });

            AGV.WagoDO.SetState(DIOModule.clsDOModule.DO_ITEM.AGV_BUSY, false);
            AGV.WagoDO.SetState(DIOModule.clsDOModule.DO_ITEM.AGV_COMPT, true);


            waitEQSignalCST = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            try
            {
                wait_eq_UL_req_OFF.Start();
                wait_eq_UL_req_OFF.Wait(waitEQSignalCST.Token);
            }
            catch (OperationCanceledException ex)
            {
                return (false, action == ACTION_TYPE.Load ? AlarmCodes.Handshake_Fail_EQ_L_REQ : AlarmCodes.Handshake_Fail_EQ_U_REQ);
            }
            try
            {
                wait_eq_ready_off.Start();
                wait_eq_ready_off.Wait(waitEQSignalCST.Token);
                AGV.WagoDO.SetState(DIOModule.clsDOModule.DO_ITEM.AGV_COMPT, false);
                AGV.WagoDO.SetState(DIOModule.clsDOModule.DO_ITEM.AGV_TR_REQ, false);
                AGV.WagoDO.SetState(DIOModule.clsDOModule.DO_ITEM.AGV_VALID, false);

                LOG.Critical("[EQ Handshake] EQ READY OFF=>Handshake Done");
                return (true, AlarmCodes.None);
            }
            catch (OperationCanceledException ex)
            {
                return (false, AlarmCodes.Handshake_Fail_EQ_READY);

            }

        }


        /// <summary>
        /// 等待EQ交握訊號 BUSY OFF＝＞表示ＡＧＶ可以退出了(模擬模式下:用CST在席有無表示是否BUSY結束 LOAD=>貨被拿走. Unload=>貨被放上來)
        /// </summary>
        private async Task<(bool eq_busy_off, AlarmCodes alarmCode)> WaitEQBusyOFF(ACTION_TYPE action)
        {
            LOG.Critical("[EQ Handshake] 等待EQ BUSY OFF");

            AGV.DirectionLighter.Flash(DIOModule.clsDOModule.DO_ITEM.AGV_DiractionLight_Right,200);
            AGV.DirectionLighter.Flash(DIOModule.clsDOModule.DO_ITEM.AGV_DiractionLight_Left, 200);

            CancellationTokenSource waitEQSignalCST = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            AGV.WagoDO.SetState(DIOModule.clsDOModule.DO_ITEM.AGV_BUSY, false);
            AGV.WagoDO.SetState(DIOModule.clsDOModule.DO_ITEM.AGV_AGV_READY, true);

            Task wait_eq_busy_ON = new Task(() =>
            {
                while (!AGV.WagoDI.GetState(DIOModule.clsDIModule.DI_ITEM.EQ_BUSY))
                {
                    if (waitEQSignalCST.IsCancellationRequested)
                        return;
                    Thread.Sleep(1);
                }
            });
            Task wait_eq_busy_OFF = new Task(() =>
            {
                while (Hs_Method != HS_METHOD.EMULATION ? AGV.WagoDI.GetState(DIOModule.clsDIModule.DI_ITEM.EQ_BUSY) : (action == ACTION_TYPE.Load ? AGV.HasAnyCargoOnAGV() : !AGV.HasAnyCargoOnAGV()))
                {
                    if (waitEQSignalCST.IsCancellationRequested)
                        return;
                    Thread.Sleep(1);
                }
            });

            if (Hs_Method != HS_METHOD.EMULATION)
            {
                try
                {
                    wait_eq_busy_ON.Start();
                    wait_eq_busy_ON.Wait(waitEQSignalCST.Token);
                }
                catch (OperationCanceledException)
                {
                    return (false, AlarmCodes.Handshake_Fail_EQ_BUSY_ON);
                }
            }

            waitEQSignalCST = new CancellationTokenSource(TimeSpan.FromSeconds(Debugger.IsAttached ? 10 : 90));

            try
            {
                wait_eq_busy_OFF.Start();
                wait_eq_busy_OFF.Wait(waitEQSignalCST.Token);
                AGV.WagoDO.SetState(DIOModule.clsDOModule.DO_ITEM.AGV_AGV_READY, false); //AGV BUSY 開始退出
                AGV.WagoDO.SetState(DIOModule.clsDOModule.DO_ITEM.AGV_BUSY, true); //AGV BUSY 開始退出
                return (true, AlarmCodes.None);
            }
            catch (OperationCanceledException)
            {
                return (false, AlarmCodes.Handshake_Fail_EQ_BUSY_OFF);

            }


        }


        #endregion
        internal bool AGVSTaskResetReqHandle(RESET_MODE mode)
        {
            AGV.Sub_Status = SUB_STATUS.IDLE;
            AGV.AGV_Reset_Flag = true;
            Task.Factory.StartNew(() => AGVC.AbortTask(mode));
            AGVS.TryTaskFeedBackAsync(AGVC.RunningTaskData, AGVC.GetCurrentTagIndexOfTrajectory(AGV.BarcodeReader.CurrentTag), TASK_RUN_STATUS.ACTION_FINISH);

            return true;
        }


    }
}
