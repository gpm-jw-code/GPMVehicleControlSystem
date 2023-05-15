using GPMVehicleControlSystem.Models.VehicleControl.DIOModule;

namespace GPMVehicleControlSystem.Models.Abstracts
{
    public interface IDIOUsagable
    {
        clsDOModule DOModule { get; set; }
        clsDIModule DIModule { get; set; }
    }
}
