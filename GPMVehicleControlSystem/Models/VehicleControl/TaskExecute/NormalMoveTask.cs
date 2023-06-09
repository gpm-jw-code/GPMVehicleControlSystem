using AGVSystemCommonNet6.AGVDispatch.Messages;
using AGVSystemCommonNet6.Alarm.VMS_ALARM;
using GPMVehicleControlSystem.Models.Buzzer;

namespace GPMVehicleControlSystem.Models.VehicleControl.TaskExecute
{
    public class NormalMoveTask : TaskBase
    {
        public override ACTION_TYPE action { get; set; } = ACTION_TYPE.None;
        public NormalMoveTask(Vehicle Agv, clsTaskDownloadData taskDownloadData) : base(Agv, taskDownloadData)
        {
        }


        public override void DirectionLighterSwitchBeforeTaskExecute()
        {
        }

        public override void LaserSettingBeforeTaskExecute()
        {
            Agv.Laser.Mode = VehicleComponent.clsLaser.LASER_MODE.Bypass;
            base.LaserSettingBeforeTaskExecute();
        }

        public override Task<(bool confirm, AlarmCodes alarm_code)> BeforeExecute()
        {
            return base.BeforeExecute();
        }


    }
}
