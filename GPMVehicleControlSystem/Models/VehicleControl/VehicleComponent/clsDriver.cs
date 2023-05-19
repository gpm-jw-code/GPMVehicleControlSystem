using AGVSystemCommonNet6.Abstracts;
using AGVSystemCommonNet6.Alarm.VMS_ALARM;
using AGVSystemCommonNet6.GPMRosMessageNet.Messages;

namespace GPMVehicleControlSystem.Models.VehicleControl.VehicleComponent
{
    public class clsDriver : CarComponent
    {
        public enum DRIVER_LOCATION
        {
            LEFT, RIGHT
        }
        public override COMPOENT_NAME component_name => COMPOENT_NAME.DRIVER;
        public DRIVER_LOCATION location = DRIVER_LOCATION.RIGHT;
        public new DriverState Data => (DriverState)StateData;
        public override STATE CheckStateDataContent()
        {

            STATE _state = STATE.NORMAL;
            DriverState _driverState = (DriverState)StateData;

            if (_driverState.state != 2 && _driverState.state != 3 && _driverState.state != 5 && _driverState.state != 7)
            {
                _state = STATE.ABNORMAL;
                AddAlarm(AlarmCodes.Wheel_Motor_Alarm);
            }
            else
            {
                RemoveAlarm(AlarmCodes.Wheel_Motor_Alarm);
            }

            return _state;

        }
    }
}
