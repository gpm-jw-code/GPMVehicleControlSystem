using RosSharp.RosBridgeClient;
using RosSharp.RosBridgeClient.MessageTypes.Geometry;
using RosSharp.RosBridgeClient.MessageTypes.Std;

namespace GPMRosMessageNet.Messages
{
    public class NavigationState : Message
    {
        public const string RosMessageName = "gpm_msgs/NavigationState";

        // publish current robot coordination in map
        public PoseStamped robotPose { get; set; }
        // publish last visited node for reporting to agvc, this node should match the task trajectory id
        public RosSharp.RosBridgeClient.MessageTypes.Std.Int32 lastVisitedNode { get; set; }
        // publish turning left or right direction. 0-straight ; 1-left ; 2-right
        public ushort robotDirect { get; set; }
        // use it to delay new assigned task time
        public ushort newPathAllowed { get; set; }
        public ushort errorCode { get; set; }
        public ushort comparisonRate { get; set; }

        public NavigationState()
        {
            this.robotPose = new PoseStamped();
            this.lastVisitedNode = new RosSharp.RosBridgeClient.MessageTypes.Std.Int32();
            this.robotDirect = 0;
            this.newPathAllowed = 0;
            this.errorCode = 0;
            this.comparisonRate = 0;
        }

        public NavigationState(PoseStamped robotPose, RosSharp.RosBridgeClient.MessageTypes.Std.Int32 lastVisitedNode, ushort robotDirect, ushort newPathAllowed, ushort errorCode, ushort comparisonRate)
        {
            this.robotPose = robotPose;
            this.lastVisitedNode = lastVisitedNode;
            this.robotDirect = robotDirect;
            this.newPathAllowed = newPathAllowed;
            this.errorCode = errorCode;
            this.comparisonRate = comparisonRate;
        }
    }
}
