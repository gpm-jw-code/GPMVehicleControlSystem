using GPMVehicleControlSystem.Models.AGVDispatch.Messages;
using GPMVehicleControlSystem.Models.VehicleControl.AGVControl;
using static GPMVehicleControlSystem.Models.VehicleControl.Vehicle;

namespace GPMVehicleControlSystem.Models.VehicleControl
{
    /// <summary>
    /// AGV駕駛員,可以控制車子(包含元件跟車控)以及跟AGVS溝通
    /// </summary>
    public class AGVPILOT
    {
        public Vehicle AGV { get; }
        /// <summary>
        /// 車控
        /// </summary>
        private CarController AGVC => AGV.CarController;
        private AGVDispatch.clsAGVSConnection AGVS => AGV.AGVSConnection;
        public AGVPILOT(Vehicle AGV)
        {
            this.AGV = AGV;
            AGVC.OnTaskActionFinishAndSuccess += AGVMoveTaskActionSuccessHandle;
            AGVC.OnTaskActionFinishCauseAbort += CarController_OnTaskActionFinishCauseAbort;
            AGVC.OnMoveTaskStart += CarController_OnMoveTaskStart;
            AGVS.OnTaskDownload += AGVSTaskDownloadConfirm;
            AGVS.OnTaskResetReq = AGVSTaskResetReqHandle;
            AGVS.OnTaskDownloadFeekbackDone += ExecuteAGVSTask;

        }

        private async void CarController_OnMoveTaskStart(object? sender, clsTaskDownloadData taskData)
        {
            AGV.Main_Status = MAIN_STATUS.RUN;
            AGV.Sub_Status = SUB_STATUS.RUN;
            await AGVS.TryTaskFeedBackAsync(taskData, AGVC.GetCurrentTagIndexOfTrajectory(AGV.BarcodeReader.CurrentTag), TASK_RUN_STATUS.NAVIGATING);

        }

        private void CarController_OnTaskActionFinishCauseAbort(object? sender, clsTaskDownloadData e)
        {
        }

        private async void AGVMoveTaskActionSuccessHandle(object? sender, clsTaskDownloadData taskData)
        {
            AGV.Main_Status = MAIN_STATUS.IDLE;
            AGV.Sub_Status = SUB_STATUS.IDLE;

            await Task.Delay(1200);

            if (AGV.Sub_Status == SUB_STATUS.Initialize | AGV.Sub_Status == SUB_STATUS.DOWN)
            {
                await AGVS.TryTaskFeedBackAsync(taskData, AGVC.GetCurrentTagIndexOfTrajectory(AGV.BarcodeReader.CurrentTag), TASK_RUN_STATUS.ACTION_FINISH);
                return;
            }

            try
            {

                bool isActionFinish = AGV.Navigation.Data.lastVisitedNode.data == taskData.Destination;
                TASK_RUN_STATUS task_status = isActionFinish ? TASK_RUN_STATUS.ACTION_FINISH : TASK_RUN_STATUS.NAVIGATING;
                await AGVS.TryTaskFeedBackAsync(taskData, AGVC.GetCurrentTagIndexOfTrajectory(AGV.BarcodeReader.CurrentTag), TASK_RUN_STATUS.ACTION_FINISH);

            }
            catch (Exception ex)
            {
                LOG.ERROR("AGVMoveTaskActionSuccessHandle", ex);
            }
        }

        internal bool AGVSTaskDownloadConfirm(clsTaskDownloadData taskDownloadData)
        {
            AGV.AGV_Reset_Flag = false;
            return true;
        }



        internal bool AGVSTaskResetReqHandle(RESET_MODE mode)
        {
            AGV.Main_Status = MAIN_STATUS.DOWN;
            AGV.Sub_Status = SUB_STATUS.DOWN;
            AGV.AGV_Reset_Flag = true;
            Task.Factory.StartNew(() => AGVC.AbortTask(mode));
            AGVS.TryTaskFeedBackAsync(AGVC.RunningTaskData, AGVC.GetCurrentTagIndexOfTrajectory(AGV.BarcodeReader.CurrentTag), TASK_RUN_STATUS.ACTION_FINISH);

            return true;
        }

        internal void ExecuteAGVSTask(object? sender, clsTaskDownloadData taskDownloadData)
        {
            LOG.INFO($"Task Download: Task Name = {taskDownloadData.Task_Name} , Task Simple = {taskDownloadData.Task_Simplex}");

            Task.Run(async () =>
            {


                await Task.Delay(300);
                bool agv_running = await AGVC.AGVSTaskDownloadHandler(taskDownloadData);
            });
        }
    }
}
