

using RosSharp.RosBridgeClient;

namespace GPMRosMessageNet.Services
{
    public class ResetMileageResponse : Message
    {
        public const string RosMessageName = "gpm_msgs/ResetMileage";

        public bool confirm { get; set; }

        public ResetMileageResponse()
        {
            this.confirm = false;
        }

        public ResetMileageResponse(bool confirm)
        {
            this.confirm = confirm;
        }
    }
}
