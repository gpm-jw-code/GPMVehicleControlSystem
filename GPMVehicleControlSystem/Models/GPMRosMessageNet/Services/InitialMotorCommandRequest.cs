

using RosSharp.RosBridgeClient;



namespace GPMRosMessageNet.Services
{
    public class InitialMotorCommandRequest : Message
    {
        public const string RosMessageName = "gpm_msgs/InitialMotorCommand";

        public string model { get; set; }
        public string command { get; set; } = "init";

        public InitialMotorCommandRequest()
        {
            this.model = "";
            this.command = "init";
        }

        public InitialMotorCommandRequest(string model, string command)
        {
            this.model = model;
            this.command = command;
        }
    }
}
