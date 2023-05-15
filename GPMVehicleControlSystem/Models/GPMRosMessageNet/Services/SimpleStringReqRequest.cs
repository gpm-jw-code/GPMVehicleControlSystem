

using RosSharp.RosBridgeClient;



namespace GPMRosMessageNet.Services
{
    public class SimpleStringReqRequest : Message
    {
        public const string RosMessageName = "gpm_msgs/SimpleStringReq";

        public string request { get; set; }

        public SimpleStringReqRequest()
        {
            this.request = "";
        }

        public SimpleStringReqRequest(string request)
        {
            this.request = request;
        }
    }
}
