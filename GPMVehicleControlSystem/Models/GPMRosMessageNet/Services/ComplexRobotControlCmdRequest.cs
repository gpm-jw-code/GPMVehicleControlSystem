using RosSharp.RosBridgeClient;
namespace GPMRosMessageNet.Services
{
    public class ComplexRobotControlCmdRequest : Message
    {
        public const string RosMessageName = "gpm_msgs/ComplexRobotControlCmd";

        public byte reqsrv { get; set; }
        public string taskID { get; set; }

        public ComplexRobotControlCmdRequest()
        {
            this.reqsrv = 0;
            this.taskID = "";
        }

        public ComplexRobotControlCmdRequest(byte reqsrv, string taskID)
        {
            this.reqsrv = reqsrv;
            this.taskID = taskID;
        }
    }
}
