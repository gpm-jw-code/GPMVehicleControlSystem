using AGVSystemCommonNet6.AGVDispatch.Messages;
using AGVSystemCommonNet6.Alarm.VMS_ALARM;
using AGVSystemCommonNet6.GPMRosMessageNet.Messages;
using AGVSystemCommonNet6.Log;
using GPMVehicleControlSystem.Models.VehicleControl.AGVControl;
using GPMVehicleControlSystem.Tools;
using GPMVehicleControlSystem.VehicleControl.DIOModule;
using System.Diagnostics;
using static AGVSystemCommonNet6.clsEnums;
using static GPMVehicleControlSystem.Models.VehicleControl.Vehicle;
using static GPMVehicleControlSystem.Models.VehicleControl.VehicleComponent.clsLaser;

namespace GPMVehicleControlSystem.Models.VehicleControl
{
    public partial class Vehicle
    {
       
        private void LaserSettingByActionType(ACTION_TYPE action)
        {
            //Laser模式變更

            if (action == ACTION_TYPE.Charge | action == ACTION_TYPE.Unload | action == ACTION_TYPE.Load | action == ACTION_TYPE.Discharge)
            {
                Laser.Mode = LASER_MODE.Loading;

            }
            else
                Laser.Mode = LASER_MODE.Move;

        }

    }
}
