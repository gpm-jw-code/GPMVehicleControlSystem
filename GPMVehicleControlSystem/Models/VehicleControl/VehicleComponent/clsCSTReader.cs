using AGVSystemCommonNet6.Abstracts;
using AGVSystemCommonNet6.GPMRosMessageNet.Messages;
using AGVSystemCommonNet6.Alarm.VMS_ALARM;
using RosSharp.RosBridgeClient;

namespace GPMVehicleControlSystem.Models.VehicleControl.VehicleComponent
{
    public partial class clsCSTReader : CarComponent
    {
        public override COMPOENT_NAME component_name => COMPOENT_NAME.CST_READER;
        public new CSTReaderState Data => StateData == null ? new CSTReaderState() : (CSTReaderState)StateData;
        public override STATE CheckStateDataContent()
        {
            STATE _state = STATE.NORMAL;

            if (Data.state != 1)
            {
                _state = STATE.ABNORMAL;
                AddAlarm(AlarmCodes.Read_Cst_ID_Fail);
            }
            else
            {
                RemoveAlarm(AlarmCodes.Read_Cst_ID_Fail);
            }

            return _state;
        }

    }


}
