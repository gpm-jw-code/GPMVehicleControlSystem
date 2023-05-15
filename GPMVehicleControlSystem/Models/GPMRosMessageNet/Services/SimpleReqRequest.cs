

using RosSharp.RosBridgeClient;



namespace GPMRosMessageNet.Services
{
    public class SimpleReqRequest : Message
    {
        public const string RosMessageName = "gpm_msgs/SimpleReq";

        public bool request { get; set; }

        public SimpleReqRequest()
        {
            this.request = false;
        }

        public SimpleReqRequest(bool request)
        {
            this.request = request;
        }
    }
}
