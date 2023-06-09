using AGVSystemCommonNet6.AGVDispatch.Messages;
using AGVSystemCommonNet6.Alarm.VMS_ALARM;
using static AGVSystemCommonNet6.clsEnums;

namespace GPMVehicleControlSystem.Models.VehicleControl.TaskExecute
{
    public class ParkTask : TaskBase
    {
        public override ACTION_TYPE action { get; set; } = ACTION_TYPE.Park;
        public ParkTask(Vehicle Agv, clsTaskDownloadData taskDownloadData) : base(Agv, taskDownloadData)
        {
        }

        public override Task<(bool confirm, AlarmCodes alarm_code)> BeforeExecute()
        {
            Agv.Laser.LeftLaserBypass = Agv.Laser.RightLaserBypass = true;
            return base.BeforeExecute();
        }

        public override async Task<(bool confirm, AlarmCodes alarm_code)> AfterMoveDone()
        {
            Agv.Sub_Status = SUB_STATUS.IDLE;
            Agv.FeedbackTaskStatus(TASK_RUN_STATUS.ACTION_FINISH);
            return (true, AlarmCodes.None);
        }

        public override void DirectionLighterSwitchBeforeTaskExecute()
        {
        }
    }
}
