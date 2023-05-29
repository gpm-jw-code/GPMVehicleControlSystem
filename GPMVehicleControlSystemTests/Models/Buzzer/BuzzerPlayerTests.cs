using Microsoft.VisualStudio.TestTools.UnitTesting;
using GPMVehicleControlSystem.Models.Buzzer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RosSharp.RosBridgeClient;
using RosSharp.RosBridgeClient.Protocols;

namespace GPMVehicleControlSystem.Models.Buzzer.Tests
{
    [TestClass()]
    public class BuzzerPlayerTests
    {
        [TestMethod()]
        public void PlayWithRosServiceTest()
        {
            RosSocket rosSocket = new RosSocket(new RosSharp.RosBridgeClient.Protocols.WebSocketSharpProtocol($"ws://192.168.235.128:9090"));
            BuzzerPlayer.rossocket = rosSocket;
            BuzzerPlayer.PlayWithRosService("/home/jinwei/param/sounds/action.wav");
        }
    }
}