using Microsoft.VisualStudio.TestTools.UnitTesting;
using GPMVehicleControlSystem.Models.VehicleControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GPMVehicleControlSystem.Models.VehicleControl.DIOModule;
using System.Threading;
using GPMVehicleControlSystem.Models.VehicleControl.VehicleComponent;
using GPMVehicleControlSystem.VehicleControl.DIOModule;

namespace GPMVehicleControlSystem.Models.VehicleControl.Tests
{
    [TestClass()]
    public class clsDirectionLighterTests
    {
        private static clsDOModule doModule = new clsDOModule();
        private static clsDirectionLighter directionLighter;
        [TestInitialize()]
        public void clsDirectionLighterTest()
        {
            directionLighter = new clsDirectionLighter(doModule);
        }

        [TestMethod()]
        public void CloseAllTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void OpenAllTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void TurnRightTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void TurnLeftTest()
        {
            directionLighter.TurnLeft();
            Thread.Sleep(5000);
        }

        [TestMethod()]
        public void ForwardTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void BackwardTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void EmergencyTest()
        {
            Assert.Fail();
        }
    }
}