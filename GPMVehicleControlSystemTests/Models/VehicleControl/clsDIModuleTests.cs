using GPMVehicleControlSystem.VehicleControl.DIOModule;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;

namespace GPMVehicleControlSystem.Models.VehicleControl.Tests
{
    [TestClass()]
    public class clsDIModuleTests
    {
        [TestMethod()]
        public void StartAsyncTest()
        {
            clsDIModule wago_di = new clsDIModule("12.12.12.12", 12333);
            wago_di.StartAsync();
            while (true)
            {
                Thread.Sleep(1);
            };
        }
    }
}