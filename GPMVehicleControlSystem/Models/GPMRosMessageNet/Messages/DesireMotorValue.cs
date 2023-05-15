

using RosSharp.RosBridgeClient;


namespace GPMRosMessageNet.Messages
{
    public class DesireMotorValue : Message
    {
        public const string RosMessageName = "gpm_msgs/DesireMotorValue";

        public double[] rpm { get; set; }
        public double[] ang { get; set; }

        public DesireMotorValue()
        {
            this.rpm = new double[0];
            this.ang = new double[0];
        }

        public DesireMotorValue(double[] rpm, double[] ang)
        {
            this.rpm = rpm;
            this.ang = ang;
        }
    }
}
