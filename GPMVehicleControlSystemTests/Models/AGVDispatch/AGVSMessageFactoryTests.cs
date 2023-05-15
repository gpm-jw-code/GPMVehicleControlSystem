using Microsoft.VisualStudio.TestTools.UnitTesting;
using GPMVehicleControlSystem.Models.AGVDispatch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GPMVehicleControlSystem.Models.AGVDispatch.Messages;
using Newtonsoft.Json;

namespace GPMVehicleControlSystem.Models.AGVDispatch.Tests
{
    [TestClass()]
    public class AGVSMessageFactoryTests
    {
        [TestMethod()]
        public void FormatSendOutStringTest()
        {
            string str = AGVSMessageFactory.FormatSendOutString("{}");
            byte[] _byte = Encoding.ASCII.GetBytes(str);
            Assert.AreEqual(0xD, _byte.Last());
            Assert.AreEqual("{}*\r", str);
        }

        [TestMethod()]
        public void CreateOnlineModeQueryDataTest()
        {
            byte[] data = AGVSMessageFactory.CreateOnlineModeQueryData(out clsOnlineModeQueryMessage msg);
            data = AGVSMessageFactory.CreateOnlineModeQueryData(out clsOnlineModeQueryMessage msg2);
            Assert.IsTrue(msg.SystemBytes == 1);
            Assert.IsTrue(msg2.SystemBytes == 2);
            Assert.AreEqual(0xD, data.Last());
        }
    }
}