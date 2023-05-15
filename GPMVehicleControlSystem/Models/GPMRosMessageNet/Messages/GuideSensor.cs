

using RosSharp.RosBridgeClient;


namespace GPMRosMessageNet.Messages
{
    public class GuideSensor : Message
    {
        public const string RosMessageName = "gpm_msgs/GuideSensor";

        public sbyte state1 { get; set; }
        public sbyte state2 { get; set; }
        public short[] guide1 { get; set; }
        public short[] guide2 { get; set; }

        public GuideSensor()
        {
            this.state1 = 0;
            this.state2 = 0;
            this.guide1 = new short[16];
            this.guide2 = new short[16];
        }

        public GuideSensor(sbyte state1, sbyte state2, short[] guide1, short[] guide2)
        {
            this.state1 = state1;
            this.state2 = state2;
            this.guide1 = guide1;
            this.guide2 = guide2;
        }
    }
}
