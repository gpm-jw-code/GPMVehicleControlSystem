

using RosSharp.RosBridgeClient;



namespace GPMRosMessageNet.Services
{
    public class RemoteControlSwitchCommandRequest : Message
    {
        public const string RosMessageName = "gpm_msgs/RemoteControlSwitchCommand";

        public bool remoteMode { get; set; }
        public string model { get; set; }
        public string id { get; set; }

        public RemoteControlSwitchCommandRequest()
        {
            this.remoteMode = false;
            this.model = "";
            this.id = "";
        }

        public RemoteControlSwitchCommandRequest(bool remoteMode, string model, string id)
        {
            this.remoteMode = remoteMode;
            this.model = model;
            this.id = id;
        }
    }
}
