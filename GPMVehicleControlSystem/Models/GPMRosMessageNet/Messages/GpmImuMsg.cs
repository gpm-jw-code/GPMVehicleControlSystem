

using RosSharp.RosBridgeClient;


using RosSharp.RosBridgeClient.MessageTypes.Sensor;

namespace GPMRosMessageNet.Messages
{
    public class GpmImuMsg : Message
    {
        public const string RosMessageName = "gpm_msgs/GpmImuMsg";

        public sbyte state { get; set; }
        public Imu imuData { get; set; }

        public GpmImuMsg()
        {
            this.state = 0;
            this.imuData = new Imu();
        }

        public GpmImuMsg(sbyte state, Imu imuData)
        {
            this.state = state;
            this.imuData = imuData;
        }
    }
}
