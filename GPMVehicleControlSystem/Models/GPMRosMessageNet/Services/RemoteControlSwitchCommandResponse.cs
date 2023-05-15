
using RosSharp.RosBridgeClient;
namespace GPMRosMessageNet.Services
{
    public class RemoteControlSwitchCommandResponse : Message
    {
        public const string RosMessageName = "gpm_msgs/RemoteControlSwitchCommand";

        public bool confirm { get; set; }

        public RemoteControlSwitchCommandResponse()
        {
            this.confirm = false;
        }

        public RemoteControlSwitchCommandResponse(bool confirm)
        {
            this.confirm = confirm;
        }
    }
}
