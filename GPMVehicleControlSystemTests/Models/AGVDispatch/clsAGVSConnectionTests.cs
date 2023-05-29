using Microsoft.VisualStudio.TestTools.UnitTesting;
using GPMVehicleControlSystem.Models.AGVDispatch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using AGVSystemCommonNet6.AGVDispatch;

namespace GPMVehicleControlSystem.Models.AGVDispatch.Tests
{
    [TestClass()]
    public class clsAGVSConnectionTests
    {
        [TestMethod()]
        public void clsAGVSConnectionTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void clsAGVSConnectionTest1()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void ConnectTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void DisconnectTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void IsConnectedTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void WriteDataOutTest()
        {
            clsAGVSConnection conn = new clsAGVSConnection("192.168.0.9", 12345);
            conn.Connect();
            conn.WriteDataOut(new byte[] { 1, 2, 3 });
        }

        [TestMethod()]
        public void TryOnlineModeQueryTest()
        {
            clsAGVSConnection conn = new clsAGVSConnection("192.168.0.9", 12345);
            conn.Connect();
            var result = conn.TryOnlineModeQueryAsync().Result;
         
        }
    }
}