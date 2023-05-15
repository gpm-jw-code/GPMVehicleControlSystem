

using RosSharp.RosBridgeClient;

namespace GPMRosMessageNet.Services
{
    public class TaskDoneCommandRequest : Message
    {
        public const string RosMessageName = "gpm_msgs/TaskDoneCommand";

        public string model { get; set; }
        public string command { get; set; }
        public string taskID { get; set; }

        public TaskDoneCommandRequest()
        {
            this.model = "";
            this.command = "";
            this.taskID = "";
        }

        public TaskDoneCommandRequest(string model, string command, string taskID)
        {
            this.model = model;
            this.command = command;
            this.taskID = taskID;
        }
    }
}
