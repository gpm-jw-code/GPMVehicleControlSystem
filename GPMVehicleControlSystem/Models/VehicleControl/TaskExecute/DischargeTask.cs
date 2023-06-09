using AGVSystemCommonNet6.AGVDispatch.Messages;
using AGVSystemCommonNet6.Alarm.VMS_ALARM;
using GPMVehicleControlSystem.Models.Buzzer;
using static GPMVehicleControlSystem.VehicleControl.DIOModule.clsDOModule;

namespace GPMVehicleControlSystem.Models.VehicleControl.TaskExecute
{
    public class DischargeTask : ChargeTask
    {
        public override ACTION_TYPE action { get; set; } = ACTION_TYPE.Discharge;

        public DischargeTask(Vehicle Agv, clsTaskDownloadData taskDownloadData) : base(Agv, taskDownloadData)
        {
        }
        public override void LaserSettingBeforeTaskExecute()
        {
            Agv.Laser.RightLaserBypass = Agv.Laser.LeftLaserBypass = true;
            base.LaserSettingBeforeTaskExecute();
        }

        public override void DirectionLighterSwitchBeforeTaskExecute()
        {
            Agv.DirectionLighter.Backward();
        }

        public override async Task<(bool confirm, AlarmCodes alarm_code)> BeforeExecute()
        {
            Agv.Laser.Mode = VehicleComponent.clsLaser.LASER_MODE.Loading;
            Agv.Laser.LeftLaserBypass = Agv.Laser.RightLaserBypass = true;
            Agv.WagoDO.SetState(DO_ITEM.Recharge_Circuit, false);
            return (true, AlarmCodes.None);
            //return base.BeforeExecute();
        }

    }
}
