

using RosSharp.RosBridgeClient;


namespace GPMRosMessageNet.Messages
{
    public class UltrasonicSensorState : Message
    {
        public const string RosMessageName = "gpm_msgs/UltrasonicSensorState";

        public sbyte state { get; set; }
        public byte errorCode { get; set; }
        public string errorString { get; set; }
        public byte dirFront { get; set; }
        public byte dirRear { get; set; }
        public byte dirLeft { get; set; }
        public byte dirRight { get; set; }

        public UltrasonicSensorState()
        {
            this.state = 0;
            this.errorCode = 0;
            this.errorString = "";
            this.dirFront = 0;
            this.dirRear = 0;
            this.dirLeft = 0;
            this.dirRight = 0;
        }

        public UltrasonicSensorState(sbyte state, byte errorCode, string errorString, byte dirFront, byte dirRear, byte dirLeft, byte dirRight)
        {
            this.state = state;
            this.errorCode = errorCode;
            this.errorString = errorString;
            this.dirFront = dirFront;
            this.dirRear = dirRear;
            this.dirLeft = dirLeft;
            this.dirRight = dirRight;
        }
    }
}
