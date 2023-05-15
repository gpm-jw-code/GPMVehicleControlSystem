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