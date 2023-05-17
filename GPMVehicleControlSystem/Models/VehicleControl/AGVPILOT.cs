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

        internal void ExecuteAGVSTask(object? sender, clsTaskDownloadData taskDownloadData)
        {
            LOG.INFO($"Task Download: Task Name = {taskDownloadData.Task_Name} , Task Simple = {taskDownloadData.Task_Simplex}");

            Task.Run(async () =>
            {
                await Task.Delay(100);
                ExecuteActionBeforeMoving(taskDownloadData);
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
        private void ExecuteActionBeforeMoving(clsTaskDownloadData taskDownloadData)
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
                bool Enable = AppSettingsHelper.GetValue<bool>("VCS:LOAD_OBS_DETECTION:Enable_Load");
                if (Enable)
                    StartFrontendObstcleDetection("ACTION_TYPE.Load");
            }
            else if (action == ACTION_TYPE.Unload)
            {
                bool Enable = AppSettingsHelper.GetValue<bool>("VCS:LOAD_OBS_DETECTION:Enable_Unload");
                if (Enable)
                    StartFrontendObstcleDetection("ACTION_TYPE.Unload");
            }
        }

        /// <summary>
        /// 移動任務結束後
        /// </summary>
        /// <param name="taskDownloadData"></param>
        private async void ExecuteActionAfterMoving(clsTaskDownloadData taskDownloadData)
        {
            ACTION_TYPE action = taskDownloadData.Action_Type;

            if (action == ACTION_TYPE.Load | action == ACTION_TYPE.Unload)
            {

                if (action == ACTION_TYPE.Load)
                {
                    //檢查在席全ON(車上應該要沒貨)
                    if (AGV.HasAnyCargoOnAGV())
                    {
                        Alarm.AlarmManager.AddAlarm( AlarmCodes.Has_Cst_Without_Job);
                        AGV.Sub_Status = SUB_STATUS.DOWN;
                        return;
                    }

                }

                if(action == ACTION_TYPE.Unload)
                {
                    //檢查在席全ON(車上應該要沒貨)
                    if (!AGV.HasAnyCargoOnAGV())
                    {
                        Alarm.AlarmManager.AddAlarm(AlarmCodes.Has_Job_Without_Cst);
                        AGV.Sub_Status = SUB_STATUS.DOWN;
                        return;
                    }
                }



                clsTaskDownloadData _AGVBackTaskDownloadData = taskDownloadData.TurnToBackTaskData();
                await AGVC.AGVSTaskDownloadHandler(_AGVBackTaskDownloadData);
            }

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
                    LOG.WARN($"前方二次檢Sensor觸發(第 {stopwatch.ElapsedMilliseconds / 1000.0} 秒)");
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
                        ExecuteActionAfterMoving(taskData);
                    }
                    else
                        await AGVS.TryTaskFeedBackAsync(taskData, AGVC.GetCurrentTagIndexOfTrajectory(AGV.BarcodeReader.CurrentTag), TASK_RUN_STATUS.ACTION_FINISH);

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
