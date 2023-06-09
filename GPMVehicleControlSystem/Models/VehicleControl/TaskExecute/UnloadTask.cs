using AGVSystemCommonNet6.AGVDispatch.Messages;
using AGVSystemCommonNet6.Alarm.VMS_ALARM;

namespace GPMVehicleControlSystem.Models.VehicleControl.TaskExecute
{
    public class UnloadTask : LoadTask
    {
        public UnloadTask(Vehicle Agv, clsTaskDownloadData taskDownloadData) : base(Agv, taskDownloadData)
        {
            action = ACTION_TYPE.Unload;
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
    }
}
