using Microsoft.VisualStudio.TestTools.UnitTesting;
using GPMVehicleControlSystem.Models.VehicleControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using GPMVehicleControlSystem.Models.VehicleControl.DIOModule;

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