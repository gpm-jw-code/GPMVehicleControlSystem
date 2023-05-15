using GPMRosMessageNet.Messages;
using GPMVehicleControlSystem.Models.Abstracts;
using GPMVehicleControlSystem.Models.VehicleControl.VehicleComponent.Abstracts;

namespace GPMVehicleControlSystem.Models.VehicleControl.VehicleComponent
{
    public class clsBarcodeReader : CarComponent
    {
        public override COMPOENT_NAME component_name => COMPOENT_NAME.BARCODE_READER;

        public int CurrentTag => (int)Data.tagID;
        public new BarcodeReaderState Data => (BarcodeReaderState)StateData;

        public override STATE CheckStateDataContent()
        {
            BarcodeReaderState _brState = (BarcodeReaderState)StateData;
            STATE _state = STATE.NORMAL;

            if (_brState.state == -1)
            {
                _state = STATE.ABNORMAL;
                AddAlarm(Alarm.AlarmCodes.Barcode_Module_Error);
            }
            else
            {
                RemoveAlarm(Alarm.AlarmCodes.Barcode_Module_Error);
            }

            return _state;
        }
    }
}
