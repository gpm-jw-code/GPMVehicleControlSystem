namespace GPMVehicleControlSystem.ViewModels
{
    public class BatteryStateVM
    {
        public int BatteryLevel { get; set; }
        public bool IsCharging { get; set; }
        public int ChargeCurrent { get; set; }
        public bool IsError { get; set; }
        public bool CircuitOpened { get; set; }
    }
}
