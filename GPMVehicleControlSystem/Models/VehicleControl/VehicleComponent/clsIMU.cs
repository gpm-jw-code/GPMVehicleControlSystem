using GPMRosMessageNet.Messages;
using GPMVehicleControlSystem.Models.Abstracts;
using GPMVehicleControlSystem.Models.VehicleControl.VehicleComponent.Abstracts;

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
                AddAlarm(Alarm.AlarmCodes.IMU_Module_Error);
            }
            else
            {
                RemoveAlarm(Alarm.AlarmCodes.IMU_Module_Error);
            }
            return _state;
        }
    }
}
