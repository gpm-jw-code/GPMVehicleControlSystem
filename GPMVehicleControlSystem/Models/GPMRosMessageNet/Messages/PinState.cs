

using RosSharp.RosBridgeClient;


namespace GPMRosMessageNet.Messages
{
    public class PinState : Message
    {
        public const string RosMessageName = "gpm_msgs/PinState";

        public string name { get; set; }
        public ushort state { get; set; }
        public ushort ORG_S { get; set; }
        public ushort INPOSITION { get; set; }
        public ushort READY { get; set; }
        public ushort SERVO_S { get; set; }
        public ushort ALARM { get; set; }
        public string pose { get; set; }

        public PinState()
        {
            this.name = "";
            this.state = 0;
            this.ORG_S = 0;
            this.INPOSITION = 0;
            this.READY = 0;
            this.SERVO_S = 0;
            this.ALARM = 0;
            this.pose = "";
        }

        public PinState(string name, ushort state, ushort ORG_S, ushort INPOSITION, ushort READY, ushort SERVO_S, ushort ALARM, string pose)
        {
            this.name = name;
            this.state = state;
            this.ORG_S = ORG_S;
            this.INPOSITION = INPOSITION;
            this.READY = READY;
            this.SERVO_S = SERVO_S;
            this.ALARM = ALARM;
            this.pose = pose;
        }
    }
}
