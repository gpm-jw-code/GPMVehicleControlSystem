using AGVSystemCommonNet6.AGVDispatch.Messages;
using AGVSystemCommonNet6.Alarm.VMS_ALARM;
using GPMVehicleControlSystem.Models.Buzzer;
using static AGVSystemCommonNet6.clsEnums;
using static GPMVehicleControlSystem.Models.VehicleControl.VehicleComponent.clsLaser;
using static GPMVehicleControlSystem.VehicleControl.DIOModule.clsDOModule;

namespace GPMVehicleControlSystem.Models.VehicleControl.TaskExecute
{
    public class ChargeTask : TaskBase
    {
        public override ACTION_TYPE action { get; set; } = ACTION_TYPE.Charge;

        public ChargeTask(Vehicle Agv, clsTaskDownloadData taskDownloadData) : base(Agv, taskDownloadData)
        {
        }



        public override void LaserSettingBeforeTaskExecute()
        {
            Agv.Laser.LeftLaserBypass = true;
            Agv.Laser.RightLaserBypass = true;
            Agv.Laser.Mode = LASER_MODE.Loading;
            base.LaserSettingBeforeTaskExecute();
        }

        public override Task<(bool confirm, AlarmCodes alarm_code)> BeforeExecute()
        {
            Agv.WagoDO.SetState(DO_ITEM.Recharge_Circuit, true);
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
            Agv.DirectionLighter.Forward();
        }
    }
}
