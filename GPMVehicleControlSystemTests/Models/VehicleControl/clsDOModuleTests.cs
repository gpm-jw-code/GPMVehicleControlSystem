using GPMVehicleControlSystem.VehicleControl.DIOModule;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;

namespace GPMVehicleControlSystem.Models.VehicleControl.Tests
{
    [TestClass()]
    public class clsDOModuleTests
    {
        [TestMethod()]
        public void StartTest()
        {
            clsDOModule wago_do = new clsDOModule("192.168.0.3", 12345);
            wago_do.StartAsync();
            while (true)
            {
                Thread.Sleep(1);
            }
        }
    }
}