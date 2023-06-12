using GPMVehicleControlSystem.VehicleControl.DIOModule;
using static GPMVehicleControlSystem.VehicleControl.DIOModule.clsDOModule;

namespace GPMVehicleControlSystem.VehicleControl.DIOModule
{

    public partial class clsDIModule
    {
        public clsDOModule DoModuleRef { get; }

        public bool IsRightLsrBypass => DoModuleRef.GetState(DO_ITEM.Right_LsrBypass);
        public bool IsLeftLsrBypass => DoModuleRef.GetState(DO_ITEM.Left_LsrBypass);
        public bool IsFrontLsrBypass => DoModuleRef.GetState(DO_ITEM.Front_LsrBypass);
        public bool IsBackLsrBypass => DoModuleRef.GetState(DO_ITEM.Back_LsrBypass);
    }
}
