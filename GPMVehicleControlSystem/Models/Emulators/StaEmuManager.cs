using AGVSystemCommonNet6.Log;

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
            LOG.INFO("WAGO EMU Start");
        }

        public static void StartAGVROSEmu()
        {
            agvRosEmu = new AGVROSEmulator();
            LOG.INFO("AGVC(ROS) EMU Start");
        }

        internal static void Start()
        {
            Task.Factory.StartNew(() =>
            {
                StartWagoEmu();
                StartAGVROSEmu();
            });
        }
    }
}
