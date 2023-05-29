using Microsoft.VisualStudio.TestTools.UnitTesting;
using GPMVehicleControlSystem.Models.VehicleControl.DIOModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GPMVehicleControlSystem.VehicleControl.DIOModule;

namespace GPMVehicleControlSystem.Models.VehicleControl.DIOModule.Tests
{
    [TestClass()]
    public class clsDIModuleTests
    {
        [TestMethod()]
        public void ReadIOSettingsFromIniFileTest()
        {
            clsDIModule dimodule = new clsDIModule();
            dimodule.ReadIOSettingsFromIniFile();
        }

        [TestMethod()]
        public void ReadIOSettingsFromIniFileTest1()
        {
            clsDIModule dimodule = new clsDIModule(); 
            dimodule.ReadIOSettingsFromIniFile();
        }
    }
}