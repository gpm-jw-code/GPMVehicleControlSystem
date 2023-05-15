using RosSharp.RosBridgeClient;

namespace GPMRosMessageNet.Services
{
    public class ComplexRobotControlCmdResponse : Message
    {
        public const string RosMessageName = "gpm_msgs/ComplexRobotControlCmd";

        public bool confirm { get; set; }

        public ComplexRobotControlCmdResponse()
        {
            this.confirm = false;
        }

        public ComplexRobotControlCmdResponse(bool confirm)
        {
            this.confirm = confirm;
        }
    }
}
