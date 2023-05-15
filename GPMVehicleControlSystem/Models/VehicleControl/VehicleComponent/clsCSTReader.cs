using GPMRosMessageNet.Messages;
using GPMVehicleControlSystem.Models.Abstracts;
using GPMVehicleControlSystem.Models.VehicleControl.VehicleComponent.Abstracts;

namespace GPMVehicleControlSystem.Models.VehicleControl.VehicleComponent
{
    public class clsCSTReader : CarComponent
    {
        public override COMPOENT_NAME component_name => COMPOENT_NAME.CST_READER;
        public new CSTReaderState Data => (CSTReaderState)StateData;
        public override STATE CheckStateDataContent()
        {
            STATE _state = STATE.NORMAL;

            if (Data.state != 1)
            {
                _state = STATE.ABNORMAL;
                AddAlarm(Alarm.AlarmCodes.Read_Cst_ID_Fail);
            }
            else
            {
                RemoveAlarm(Alarm.AlarmCodes.Read_Cst_ID_Fail);
            }

            return _state;
        }
    }
}
