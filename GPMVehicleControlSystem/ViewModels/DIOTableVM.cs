using GPMVehicleControlSystem.VehicleControl.DIOModule;

namespace GPMVehicleControlSystem.ViewModels
{
    public class DIOTableVM
    {
        public bool IsE84HsUseEmulator { get; set; }
        public List<clsIOSignal> Inputs { get; set; }
        public List<clsIOSignal> Outputs { get; set; }
    }
}
