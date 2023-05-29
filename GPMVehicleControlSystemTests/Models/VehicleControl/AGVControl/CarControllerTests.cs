using Microsoft.VisualStudio.TestTools.UnitTesting;
using GPMVehicleControlSystem.Models.VehicleControl.AGVControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GPMVehicleControlSystem.Models.VehicleControl.AGVControl.CarController;
using AGVSystemCommonNet6.GPMRosMessageNet.Messages.SickMsg;

namespace GPMVehicleControlSystem.Models.VehicleControl.AGVControl.Tests
{
    [TestClass()]
    public class CarControllerTests
    {
        [TestMethod()]
        public void LocalizeStateEmuTest()
        {
            LocalizationControllerResultMessage0502 LocalizationControllerResult = new LocalizationControllerResultMessage0502();
            LocalizationControllerResult.loc_status = 40;
            LOCALIZE_STATE state = (LOCALIZE_STATE)LocalizationControllerResult.loc_status;
            Assert.AreEqual(LOCALIZE_STATE.System_Error, state);
            LocalizationControllerResult.loc_status = 30;
            state = (LOCALIZE_STATE)LocalizationControllerResult.loc_status;
            Assert.AreEqual(LOCALIZE_STATE.Not_Localized, state);

        }


    }
}