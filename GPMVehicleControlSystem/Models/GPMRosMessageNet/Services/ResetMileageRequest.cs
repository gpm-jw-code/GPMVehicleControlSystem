

using RosSharp.RosBridgeClient;



namespace GPMRosMessageNet.Services
{
    public class ResetMileageRequest : Message
    {
        public const string RosMessageName = "gpm_msgs/ResetMileage";

        public byte reqsrv { get; set; }

        public ResetMileageRequest()
        {
            this.reqsrv = 0;
        }

        public ResetMileageRequest(byte reqsrv)
        {
            this.reqsrv = reqsrv;
        }
    }
}
