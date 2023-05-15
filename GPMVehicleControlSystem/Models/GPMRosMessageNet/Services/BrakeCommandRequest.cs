

using RosSharp.RosBridgeClient;


namespace GPMRosMessageNet.Services
{
    public class BrakeCommandRequest : Message
    {
        public const string RosMessageName = "gpm_msgs/BrakeCommand";

        public string model { get; set; }
        public string command { get; set; }

        public BrakeCommandRequest()
        {
            this.model = "";
            this.command = "";
        }

        public BrakeCommandRequest(string model, string command)
        {
            this.model = model;
            this.command = command;
        }
    }
}
