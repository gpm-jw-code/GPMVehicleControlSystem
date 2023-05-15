

using RosSharp.RosBridgeClient;

namespace GPMRosMessageNet.Services
{
    public class CSTReaderCommandResponse : Message
    {
        public const string RosMessageName = "gpm_msgs/CSTReaderCommand";

        public bool confirm { get; set; }

        public CSTReaderCommandResponse()
        {
            this.confirm = false;
        }

        public CSTReaderCommandResponse(bool confirm)
        {
            this.confirm = confirm;
        }
    }
}
