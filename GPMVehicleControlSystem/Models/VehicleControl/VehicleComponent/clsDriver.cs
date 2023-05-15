using GPMRosMessageNet.Messages;
using GPMVehicleControlSystem.Models.Abstracts;
using GPMVehicleControlSystem.Models.VehicleControl.VehicleComponent.Abstracts;

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
                AddAlarm(Alarm.AlarmCodes.Wheel_Motor_Alarm);
            }
            else
            {
                RemoveAlarm(Alarm.AlarmCodes.Wheel_Motor_Alarm);
            }

            return _state;

        }
    }
}
