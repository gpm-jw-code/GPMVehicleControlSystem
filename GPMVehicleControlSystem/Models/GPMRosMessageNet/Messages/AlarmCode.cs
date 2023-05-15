

using RosSharp.RosBridgeClient;


namespace GPMRosMessageNet.Messages
{
    public class AlarmCodeMsg : Message
    {
        public const string RosMessageName = "gpm_msgs/AlarmCode";

        public int AlarmCode { get; set; }
        public int Level { get; set; }

        public AlarmCodeMsg()
        {
            this.AlarmCode = 0;
            this.Level = 0;
        }

        public AlarmCodeMsg(int AlarmCode, int Level)
        {
            this.AlarmCode = AlarmCode;
            this.Level = Level;
        }
    }
}
