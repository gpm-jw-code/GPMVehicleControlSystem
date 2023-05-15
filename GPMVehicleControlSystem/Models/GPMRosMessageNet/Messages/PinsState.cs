

using RosSharp.RosBridgeClient;


using GPMRosMessageNet.Messages;

namespace GPMRosMessageNet.Messages
{
    public class PinsState : Message
    {
        public const string RosMessageName = "gpm_msgs/PinsState";

        public PinState[] PinState { get; set; }

        public PinsState()
        {
            this.PinState = new PinState[0];
        }

        public PinsState(PinState[] PinState)
        {
            this.PinState = PinState;
        }
    }
}
