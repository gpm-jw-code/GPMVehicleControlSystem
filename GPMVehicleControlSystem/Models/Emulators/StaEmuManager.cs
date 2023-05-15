namespace GPMVehicleControlSystem.Models.Emulators
{
    public class StaEmuManager
    {
        // Add services to the container.
        public static WagoEmulator wagoEmu = new WagoEmulator();
        public static AGVROSEmulator agvRosEmu;
        public static void StartWagoEmu()
        {
            wagoEmu.Connect();
        }

        public static void StartAGVROSEmu()
        {
            
            agvRosEmu = new AGVROSEmulator();
        }
    }
}
