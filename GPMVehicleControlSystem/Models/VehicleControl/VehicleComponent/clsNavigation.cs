using AGVSystemCommonNet6;
using AGVSystemCommonNet6.Abstracts;
using AGVSystemCommonNet6.GPMRosMessageNet.Messages;

namespace GPMVehicleControlSystem.Models.VehicleControl.VehicleComponent
{
    public class clsNavigation : CarComponent
    {
        public enum AGV_DIRECTION : ushort
        {
            FORWARD, LEFT, RIGHT, STOP
        }
        public override COMPOENT_NAME component_name => COMPOENT_NAME.NAVIGATION;

        public new NavigationState Data => StateData == null ? new NavigationState() : (NavigationState)StateData;

        public event EventHandler<AGV_DIRECTION> OnDirectionChanged;
        public event EventHandler<int> OnTagReach;
        public event EventHandler<int> OnTagLeave;

        private int _previousTag = 0;
        private AGV_DIRECTION _previousDirection = AGV_DIRECTION.STOP;
        public AGV_DIRECTION Direction
        {
            get => _previousDirection;
            set
            {
                if (_previousDirection != value)
                {
                    OnDirectionChanged?.Invoke(this, value);
                    _previousDirection = value;
                }
            }
        }
        public int LastVisitedTag
        {
            get => _previousTag;
            set
            {
                if (value != _previousTag)
                {
                    if (value != 0)
                        OnTagReach?.Invoke(this, value);
                    else
                        OnTagLeave?.Invoke(this, value);
                    _previousTag = value;
                }
            }
        }

        public double Angle => Data.robotPose.pose.orientation.ToTheta();

        private AGV_DIRECTION ConvertToDirection(ushort direction)
        {
            if (direction == 0)
                return AGV_DIRECTION.FORWARD;
            else if (direction == 1)
                return AGV_DIRECTION.LEFT;
            else if (direction == 2)
                return AGV_DIRECTION.RIGHT;
            else
                return AGV_DIRECTION.STOP;
        }
        public override STATE CheckStateDataContent()
        {
            Direction = ConvertToDirection(Data.robotDirect);
            LastVisitedTag = Data.lastVisitedNode.data;
            if (Data.errorCode != 0)
            {
                return STATE.ABNORMAL;
            }
            else
            {

            }
            return STATE.NORMAL;
        }
    }
}
