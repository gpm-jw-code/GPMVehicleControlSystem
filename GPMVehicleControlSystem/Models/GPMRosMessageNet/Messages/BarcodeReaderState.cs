

using RosSharp.RosBridgeClient;


namespace GPMRosMessageNet.Messages
{
    public class BarcodeReaderState : Message
    {
        public const string RosMessageName = "gpm_msgs/BarcodeReaderState";

        // -1- error, 0-normal but read nothing, 1-get tag, 2-gat color tape
        public sbyte state { get; set; }
        public uint tagID { get; set; }
        public double xValue { get; set; }
        public double yValue { get; set; }
        public double theta { get; set; }

        internal int Theta_Int => (int)theta;

        public BarcodeReaderState()
        {
            this.state = 0;
            this.tagID = 0;
            this.xValue = 0.0;
            this.yValue = 0.0;
            this.theta = 0.0;
        }

        public BarcodeReaderState(sbyte state, uint tagID, double xValue, double yValue, double theta)
        {
            this.state = state;
            this.tagID = tagID;
            this.xValue = xValue;
            this.yValue = yValue;
            this.theta = theta;
        }
    }
}
