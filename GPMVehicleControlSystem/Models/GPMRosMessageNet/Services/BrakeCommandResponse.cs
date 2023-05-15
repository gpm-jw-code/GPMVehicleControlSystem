


using RosSharp.RosBridgeClient;

namespace GPMRosMessageNet.Services
{
    public class BrakeCommandResponse : Message
    {
        public const string RosMessageName = "gpm_msgs/BrakeCommand";

        public bool confirm { get; set; }

        public BrakeCommandResponse()
        {
            this.confirm = false;
        }

        public BrakeCommandResponse(bool confirm)
        {
            this.confirm = confirm;
        }
    }
}
