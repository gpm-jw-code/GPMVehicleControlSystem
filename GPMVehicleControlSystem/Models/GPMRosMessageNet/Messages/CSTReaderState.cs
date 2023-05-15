

using RosSharp.RosBridgeClient;


namespace GPMRosMessageNet.Messages
{
    public class CSTReaderState : Message
    {
        public const string RosMessageName = "gpm_msgs/CSTReaderState";

        public string name { get; set; }
        public string data { get; set; }
        public ushort state { get; set; }

        public CSTReaderState()
        {
            this.name = "";
            this.data = "";
            this.state = 0;
        }

        public CSTReaderState(string name, string data, ushort state)
        {
            this.name = name;
            this.data = data;
            this.state = state;
        }
    }
}
