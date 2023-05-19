using AGVSystemCommonNet6.Abstracts;
using AGVSystemCommonNet6.Alarm.VMS_ALARM;
using AGVSystemCommonNet6.GPMRosMessageNet.Messages;

namespace GPMVehicleControlSystem.Models.VehicleControl.VehicleComponent
{
    public class clsBarcodeReader : CarComponent
    {
        public override COMPOENT_NAME component_name => COMPOENT_NAME.BARCODE_READER;

        public new BarcodeReaderState Data => (BarcodeReaderState)StateData;
        public int CurrentTag => Data == null ? 0 : (int)Data.tagID;

        public override STATE CheckStateDataContent()
        {
            BarcodeReaderState _brState = (BarcodeReaderState)StateData;
            STATE _state = STATE.NORMAL;

            if (_brState.state == -1)
            {
                _state = STATE.ABNORMAL;
                AddAlarm(AlarmCodes.Barcode_Module_Error);
            }
            else
            {
                RemoveAlarm(AlarmCodes.Barcode_Module_Error);
            }

            return _state;
        }
    }
}
