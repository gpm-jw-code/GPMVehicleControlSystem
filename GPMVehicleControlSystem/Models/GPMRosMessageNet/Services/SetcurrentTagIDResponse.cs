

using RosSharp.RosBridgeClient;

namespace GPMRosMessageNet.Services
{
    public class SetcurrentTagIDResponse : Message
    {
        public const string RosMessageName = "gpm_msgs/SetcurrentTagID";

        public bool confirm { get; set; }

        public SetcurrentTagIDResponse()
        {
            this.confirm = false;
        }

        public SetcurrentTagIDResponse(bool confirm)
        {
            this.confirm = confirm;
        }
    }
}
