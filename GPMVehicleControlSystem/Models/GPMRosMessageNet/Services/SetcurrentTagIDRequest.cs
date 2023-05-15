

using RosSharp.RosBridgeClient;



namespace GPMRosMessageNet.Services
{
    public class SetcurrentTagIDRequest : Message
    {
        public const string RosMessageName = "gpm_msgs/SetcurrentTagID";

        public ushort tagID { get; set; }
        public string map { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double angle { get; set; }

        public SetcurrentTagIDRequest()
        {
            this.tagID = 0;
            this.map = "";
            this.X = 0.0;
            this.Y = 0.0;
            this.angle = 0.0;
        }

        public SetcurrentTagIDRequest(ushort tagID, string map, double X, double Y, double angle)
        {
            this.tagID = tagID;
            this.map = map;
            this.X = X;
            this.Y = Y;
            this.angle = angle;
        }
    }
}
