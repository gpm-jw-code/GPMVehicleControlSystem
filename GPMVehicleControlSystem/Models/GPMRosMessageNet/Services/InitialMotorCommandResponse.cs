

using RosSharp.RosBridgeClient;



namespace GPMRosMessageNet.Services
{
    public class InitialMotorCommandResponse : Message
    {
        public const string RosMessageName = "gpm_msgs/InitialMotorCommand";

        public bool confirm { get; set; }

        public InitialMotorCommandResponse()
        {
            this.confirm = false;
        }

        public InitialMotorCommandResponse(bool confirm)
        {
            this.confirm = confirm;
        }
    }
}
