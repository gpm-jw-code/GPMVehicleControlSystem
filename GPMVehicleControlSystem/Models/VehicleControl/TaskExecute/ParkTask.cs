using AGVSystemCommonNet6.AGVDispatch.Messages;
using AGVSystemCommonNet6.Alarm.VMS_ALARM;

namespace GPMVehicleControlSystem.Models.VehicleControl.TaskExecute
{
    public class ParkTask : TaskBase
    {
        public ParkTask(Vehicle Agv, clsTaskDownloadData taskDownloadData) : base(Agv, taskDownloadData)
        {
        }

        public override ACTION_TYPE action { get; set; } = ACTION_TYPE.Park;

        public override Task<(bool confirm, AlarmCodes alarm_code)> BeforeExecute()
        {
            Agv.Laser.LeftLaserBypass = Agv.Laser.RightLaserBypass = true;
            Buzzer.BuzzerPlayer.BuzzerAction();
            return base.BeforeExecute();
        }


    }
}
