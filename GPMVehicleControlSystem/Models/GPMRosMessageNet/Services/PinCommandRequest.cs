
using RosSharp.RosBridgeClient;
namespace GPMRosMessageNet.Services
{
    public class PinCommandRequest : Message
    {
        public const string RosMessageName = "gpm_msgs/PinCommand";

        public string model { get; set; }
        public string command { get; set; }
        public double target { get; set; }

        public PinCommandRequest()
        {
            this.model = "";
            this.command = "";
            this.target = 0.0;
        }

        public PinCommandRequest(string model, string command, double target)
        {
            this.model = model;
            this.command = command;
            this.target = target;
        }
    }
}
