using Microsoft.VisualStudio.TestTools.UnitTesting;
using GPMVehicleControlSystem.Models.VehicleControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GPMVehicleControlSystem.Models.VehicleControl.DIOModule;
using GPMVehicleControlSystem.Models.VehicleControl.VehicleComponent;
using GPMVehicleControlSystem.VehicleControl.DIOModule;

namespace GPMVehicleControlSystem.Models.VehicleControl.Tests
{
    [TestClass()]
    public class clsStatusLighterTests
    {
        private static clsDOModule doModule = new clsDOModule();
        private static clsStatusLighter statusLighter;
        [TestInitialize]
        public void TestInitialize()
        {
            statusLighter = new clsStatusLighter(doModule);
        }


        [TestMethod()]
        public void CloseAllTest()
        {
            statusLighter.CloseAll();
            Assert.IsFalse(doModule.GetState(clsDOModule.DO_ITEM.AGV_DiractionLight_B));
            Assert.IsFalse(doModule.GetState(clsDOModule.DO_ITEM.AGV_DiractionLight_R));
            Assert.IsFalse(doModule.GetState(clsDOModule.DO_ITEM.AGV_DiractionLight_G));
            Assert.IsFalse(doModule.GetState(clsDOModule.DO_ITEM.AGV_DiractionLight_Y));
        }

        [TestMethod()]
        public void OpenAllTest()
        {
            statusLighter.OpenAll();
            Assert.IsTrue(doModule.GetState(clsDOModule.DO_ITEM.AGV_DiractionLight_B));
            Assert.IsTrue(doModule.GetState(clsDOModule.DO_ITEM.AGV_DiractionLight_R));
            Assert.IsTrue(doModule.GetState(clsDOModule.DO_ITEM.AGV_DiractionLight_G));
            Assert.IsTrue(doModule.GetState(clsDOModule.DO_ITEM.AGV_DiractionLight_Y));
        }

        [TestMethod()]
        public void RUNTest()
        {
            statusLighter.RUN();
            Assert.IsTrue(doModule.GetState(clsDOModule.DO_ITEM.AGV_DiractionLight_G));
            Assert.IsFalse(doModule.GetState(clsDOModule.DO_ITEM.AGV_DiractionLight_R));
            Assert.IsFalse(doModule.GetState(clsDOModule.DO_ITEM.AGV_DiractionLight_Y));
        }

        [TestMethod()]
        public void DOWNTest()
        {
            statusLighter.DOWN();
            Assert.IsFalse(doModule.GetState(clsDOModule.DO_ITEM.AGV_DiractionLight_G));
            Assert.IsTrue(doModule.GetState(clsDOModule.DO_ITEM.AGV_DiractionLight_R));
            Assert.IsFalse(doModule.GetState(clsDOModule.DO_ITEM.AGV_DiractionLight_Y));
        }

        [TestMethod()]
        public void IDLETest()
        {
            statusLighter.IDLE();
            Assert.IsFalse(doModule.GetState(clsDOModule.DO_ITEM.AGV_DiractionLight_G));
            Assert.IsFalse(doModule.GetState(clsDOModule.DO_ITEM.AGV_DiractionLight_R));
            Assert.IsTrue(doModule.GetState(clsDOModule.DO_ITEM.AGV_DiractionLight_Y));
        }

        [TestMethod()]
        public void ONLINETest()
        {
            statusLighter.ONLINE();
            Assert.IsTrue(doModule.GetState(clsDOModule.DO_ITEM.AGV_DiractionLight_B));

        }

        [TestMethod()]
        public void OFFLINETest()
        {
            statusLighter.OFFLINE();
            Assert.IsFalse(doModule.GetState(clsDOModule.DO_ITEM.AGV_DiractionLight_B));
        }
    }
}