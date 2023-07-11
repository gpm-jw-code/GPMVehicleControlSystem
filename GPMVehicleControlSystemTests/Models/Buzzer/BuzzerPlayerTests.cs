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
        string rosbridge_host = "ws://192.168.235.128:9090";
        [TestMethod()]
        public void PlayAlarmTest()
        {
            RosSocket rosSocket = new RosSocket(new WebSocketSharpProtocol(rosbridge_host));
            BuzzerPlayer.rossocket = rosSocket;
            BuzzerPlayer.Play(SOUNDS.Alarm);
        }
        [TestMethod()]
        public void PlayMoveTest()
        {
            RosSocket rosSocket = new RosSocket(new WebSocketSharpProtocol(rosbridge_host));
            BuzzerPlayer.rossocket = rosSocket;
            BuzzerPlayer.Play(SOUNDS.Move);
        }
        [TestMethod()]
        public void PlayActionTest()
        {
            RosSocket rosSocket = new RosSocket(new WebSocketSharpProtocol(rosbridge_host));
            BuzzerPlayer.rossocket = rosSocket;
            BuzzerPlayer.Play(SOUNDS.Action);
        }
    }
}