using AGVSystemCommonNet6.AGVDispatch.Messages;
using AGVSystemCommonNet6.Alarm.VMS_ALARM;
using GPMVehicleControlSystem.Models.Buzzer;
using static GPMVehicleControlSystem.Models.VehicleControl.VehicleComponent.clsLaser;
using static GPMVehicleControlSystem.VehicleControl.DIOModule.clsDOModule;

namespace GPMVehicleControlSystem.Models.VehicleControl.TaskExecute
{
    public class ChargeTask : TaskBase
    {
        public ChargeTask(Vehicle Agv, clsTaskDownloadData taskDownloadData) : base(Agv, taskDownloadData)
        {
        }

        public override ACTION_TYPE action { get; set; } = ACTION_TYPE.Charge;


        public override void LaserSettingBeforeTaskExecute()
        {
            Agv.Laser.LeftLaserBypass = true;
            Agv.Laser.RightLaserBypass = true;
            Agv.Laser.Mode = LASER_MODE.Loading;
            base.LaserSettingBeforeTaskExecute();
        }
        public override Task<(bool confirm, AlarmCodes alarm_code)> AfterMoveDone()
        {
            return base.AfterMoveDone();
        }
        public override Task<(bool confirm, AlarmCodes alarm_code)> BeforeExecute()
        {
            BuzzerPlayer.BuzzerAction();
            Agv.WagoDO.SetState(DO_ITEM.Recharge_Circuit, true);
            return base.BeforeExecute();
        }

    }
}
