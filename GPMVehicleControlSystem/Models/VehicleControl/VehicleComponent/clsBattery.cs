using AGVSystemCommonNet6.Abstracts;
using AGVSystemCommonNet6.Alarm.VMS_ALARM;
using AGVSystemCommonNet6.GPMRosMessageNet.Messages;

namespace GPMVehicleControlSystem.Models.VehicleControl.VehicleComponent
{
    public class clsBattery : CarComponent
    {
        public bool IsCharging => Data.dischargeCurrent == 0 && Data.chargeCurrent != 0;

        public clsStateCheckSpec ChargingCheckSpec = new clsStateCheckSpec { };

        public clsStateCheckSpec DischargeCheckSpec = new clsStateCheckSpec
        {
            MaxCurrentAllow = 5000
        };
        public new BatteryState Data => StateData == null ? new BatteryState() : (BatteryState)StateData;

        public override COMPOENT_NAME component_name => COMPOENT_NAME.BATTERY;

        public override STATE CheckStateDataContent()
        {

            STATE _state = STATE.NORMAL;
            if (Data.batteryLevel == 0 | Data.state != 1)
            {
                _state = STATE.ABNORMAL;
                AddAlarm(AlarmCodes.Cant_Check_Battery);
            }
            else
            {
                RemoveAlarm(AlarmCodes.Cant_Check_Battery);

                if (IsCharging)
                {
                    if (Data.chargeCurrent < ChargingCheckSpec.MinCurrentAllow)
                    {
                        _state = STATE.WARNING;
                        AddAlarm(AlarmCodes.Under_Current_Charge);
                    }
                    else
                        RemoveAlarm(AlarmCodes.Under_Current_Charge);
                    if (Data.chargeCurrent > ChargingCheckSpec.MaxCurrentAllow)
                    {
                        _state = STATE.WARNING;
                        AddAlarm(AlarmCodes.Over_Current_Charge);
                    }
                    else
                        RemoveAlarm(AlarmCodes.Over_Current_Charge);
                }
                else
                {
                    if (Data.dischargeCurrent < DischargeCheckSpec.MinCurrentAllow)
                    {
                        AddAlarm(AlarmCodes.Under_Current_Discharge);
                        _state = STATE.WARNING;
                    }
                    else
                        RemoveAlarm(AlarmCodes.Under_Current_Discharge);
                    if (Data.dischargeCurrent > DischargeCheckSpec.MaxCurrentAllow)
                    {
                        AddAlarm(AlarmCodes.Over_Current_Discharge);
                        _state = STATE.WARNING;
                    }
                    else
                        RemoveAlarm(AlarmCodes.Over_Current_Discharge);
                }
            }
            return _state;
        }
    }

    public class clsStateCheckSpec
    {
        public double MinCurrentAllow { get; set; } = 80;
        public double MaxCurrentAllow { get; set; } = 4000;
        public double MinVoltageAllow { get; set; } = 20.0;
        public double MaxVoltageAllow { get; set; } = 20.0;
    }

}
