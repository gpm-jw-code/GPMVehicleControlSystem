﻿using AGVSystemCommonNet6.Abstracts;
using AGVSystemCommonNet6.Alarm.VMS_ALARM;
using AGVSystemCommonNet6.GPMRosMessageNet.Messages;

namespace GPMVehicleControlSystem.Models.VehicleControl.VehicleComponent
{
    public class clsGuideSensor : CarComponent
    {
        public override COMPOENT_NAME component_name => COMPOENT_NAME.GUID_SENSOR;

        public override STATE CheckStateDataContent()
        {
            STATE _state = STATE.NORMAL;
            GuideSensor _guide_sensor = (GuideSensor)StateData;
            if (_guide_sensor.state1 != 1 | _guide_sensor.state2 != 1)
            {
                _state = STATE.ABNORMAL;
                AddAlarm(AlarmCodes.Guide_Module_Error);
            }
            else
            {
                RemoveAlarm(AlarmCodes.Guide_Module_Error);
            }
            return _state;
        }
    }
}
