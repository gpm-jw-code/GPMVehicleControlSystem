using AGVSystemCommonNet6.Abstracts;
using AGVSystemCommonNet6.Alarm.VMS_ALARM;
using AGVSystemCommonNet6.GPMRosMessageNet.Messages;

namespace GPMVehicleControlSystem.Models.VehicleControl.VehicleComponent
{
    public class clsIMU : CarComponent
    {
        public override COMPOENT_NAME component_name => COMPOENT_NAME.IMU;

        public override STATE CheckStateDataContent()
        {
            STATE _state = STATE.NORMAL;
            GpmImuMsg _imu_state = (GpmImuMsg)base.StateData;
            if (_imu_state.state != 0)
            {
                _state = STATE.ABNORMAL;
                AddAlarm(AlarmCodes.IMU_Module_Error);
            }
            else
            {
                RemoveAlarm(AlarmCodes.IMU_Module_Error);
            }
            return _state;
        }
    }
}
