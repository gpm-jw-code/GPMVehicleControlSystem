using AGVSystemCommonNet6.Abstracts;
using AGVSystemCommonNet6.Alarm.VMS_ALARM;
using AGVSystemCommonNet6.GPMRosMessageNet.Messages;

namespace GPMVehicleControlSystem.Models.VehicleControl.VehicleComponent
{
    public class clsBarcodeReader : CarComponent
    {
        public event EventHandler<int> OnTagLeave;


        public override COMPOENT_NAME component_name => COMPOENT_NAME.BARCODE_READER;

        public new BarcodeReaderState Data => StateData == null ? new BarcodeReaderState() : (BarcodeReaderState)StateData;
        public int CurrentTag => Data == null ? 0 : (int)Data.tagID;
        private uint PreviousTag = 0;
        public override STATE CheckStateDataContent()
        {
            BarcodeReaderState _brState = (BarcodeReaderState)StateData;
            if (_brState.tagID == 0 && PreviousTag != _brState.tagID)
                OnTagLeave?.Invoke(this, (int)_brState.tagID);

            PreviousTag = _brState.tagID;

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
