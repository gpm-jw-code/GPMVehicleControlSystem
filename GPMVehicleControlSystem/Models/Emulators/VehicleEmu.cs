using AGVSystemCommonNet6.AGVDispatch.Messages;
using AGVSystemCommonNet6.AGVDispatch.Model;
using GPMVehicleControlSystem.VehicleControl.DIOModule;

namespace GPMVehicleControlSystem.Models.Emulators
{
    public class VehicleEmu
    {
        public VehicleEmu(int init_tag)
        {
            Runstatus = new RunningStatus();
            Runstatus.AGV_Status = AGVSystemCommonNet6.clsEnums.MAIN_STATUS.IDLE;
            Runstatus.Electric_Volume = new double[1] { 99.1 };
            Runstatus.Last_Visited_Node = init_tag;
        }


        public RunningStatus Runstatus;

        public void SwitchON()
        {
            StaEmuManager.wagoEmu.SetState(clsDIModule.DI_ITEM.Horizon_Motor_Switch, true);
        }

    }
}
