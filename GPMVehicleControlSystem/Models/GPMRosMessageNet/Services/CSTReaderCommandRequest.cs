

using RosSharp.RosBridgeClient;



namespace GPMRosMessageNet.Services
{
    public class CSTReaderCommandRequest : Message
    {
        public const string RosMessageName = "gpm_msgs/CSTReaderCommand";

        public string model { get; set; }
        public string command { get; set; }

        public CSTReaderCommandRequest()
        {
            this.model = "FORK";
            this.command = "read";
        }

        public CSTReaderCommandRequest(string model, string command)
        {
            this.model = model;
            this.command = command;
        }
    }
}
