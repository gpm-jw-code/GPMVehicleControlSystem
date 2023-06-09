using AGVSystemCommonNet6.AGVDispatch.Messages;
using AGVSystemCommonNet6.Alarm.VMS_ALARM;
using GPMVehicleControlSystem.Tools;

namespace GPMVehicleControlSystem.Models.VehicleControl.TaskExecute
{
    public class UnloadTask : LoadTask
    {
        public override ACTION_TYPE action { get; set; } = ACTION_TYPE.Unload;

        public UnloadTask(Vehicle Agv, clsTaskDownloadData taskDownloadData) : base(Agv, taskDownloadData)
        {
        }

        protected override async Task<(bool confirm, AlarmCodes alarmCode)> CSTBarcodeReadBeforeAction()
        {
            return (true, AlarmCodes.None);
        }

        protected override async Task<(bool confirm, AlarmCodes alarmCode)> CSTBarcodeReadAfterAction()
        {
            if (!CSTTrigger)
                return (true, AlarmCodes.None);
            return await base.CSTBarcodeRead();
        }

        /// <summary>
        /// 準備Unload(取貨)=>車上應該無貨
        /// </summary>
        /// <returns></returns>
        protected override (bool confirm, AlarmCodes alarmCode) CstExistCheckBeforeHSStartInFrontOfEQ()
        {
            if (!AppSettingsHelper.GetValue<bool>("VCS:CST_EXIST_DETECTION:Before_In"))
                return (true, AlarmCodes.None);

            if (Agv.HasAnyCargoOnAGV())
                return (false, AlarmCodes.Has_Cst_Without_Job);

            return (true, AlarmCodes.None);
        }

        /// <summary>
        ///  Unload(取貨)完成後=>車上應該有貨
        /// </summary>
        /// <returns></returns>
        protected override (bool confirm, AlarmCodes alarmCode) CstExistCheckAfterEQBusyOff()
        {
            if (!AppSettingsHelper.GetValue<bool>("VCS:CST_EXIST_DETECTION:After_EQ_Busy_Off"))
                return (true, AlarmCodes.None);

            if (!Agv.HasAnyCargoOnAGV())
                return (false, AlarmCodes.Has_Job_Without_Cst);

            return (true, AlarmCodes.None);
        }

        protected override void StartFrontendObstcleDetection()
        {
            FrontendSecondarSensorTriggerAlarmCode = AlarmCodes.EQP_UNLOAD_BUT_EQP_HAS_NO_CARGO;
            base.StartFrontendObstcleDetection();
        }
    }
}
