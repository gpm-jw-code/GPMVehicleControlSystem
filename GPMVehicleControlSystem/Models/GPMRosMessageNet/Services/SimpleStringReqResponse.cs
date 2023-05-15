

using RosSharp.RosBridgeClient;



namespace GPMRosMessageNet.Services
{
    public class SimpleStringReqResponse : Message
    {
        public const string RosMessageName = "gpm_msgs/SimpleStringReq";

        public string response { get; set; }

        public SimpleStringReqResponse()
        {
            this.response = "";
        }

        public SimpleStringReqResponse(string response)
        {
            this.response = response;
        }
    }
}
