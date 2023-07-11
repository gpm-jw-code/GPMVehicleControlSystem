using Microsoft.VisualStudio.TestTools.UnitTesting;
using GPMVehicleControlSystem.VehicleControl.DIOModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPMVehicleControlSystem.VehicleControl.DIOModule.Tests
{
    [TestClass()]
    public class clsDOModuleTests
    {
        [TestMethod()]
        public void clsDOModuleTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void clsDOModuleTest1()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void clsDOModuleTest2()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void ReadIOSettingsFromIniFileTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void ResetMotorTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void SetStateTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void SetStateTest1()
        {
            clsDOModule module = new clsDOModule();
            module.SetState(clsDOModule.DO_ITEM.AGV_DiractionLight_B, true);

            Assert.Fail();
        }

        [TestMethod()]
        public void GetStateTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void ResetHandshakeSignalsTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void StartAsyncTest()
        {
            Assert.Fail();
        }
    }
}