using RosSharp.RosBridgeClient;


namespace GPMRosMessageNet.Services
{
    public class VerticalCommandRequest : Message
    {
        public const string RosMessageName = "gpm_msgs/VerticalCommand";

        public string model { get; set; }
        public string command { get; set; }
        public double target { get; set; }
        public double speed { get; set; }


        public VerticalCommandRequest()
        {
            this.model = "";
            this.command = "";
            this.target = 0.0;
            this.speed = 1;
        }

        public VerticalCommandRequest(string model, string command, double target, double speed)
        {
            this.model = model;
            this.command = command;
            this.target = target;
            this.speed = speed;
        }
    }
}
