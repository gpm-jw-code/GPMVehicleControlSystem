using AGVSystemCommonNet6;
using AGVSystemCommonNet6.Abstracts;
using AGVSystemCommonNet6.GPMRosMessageNet.Messages;
using RosSharp.RosBridgeClient.MessageTypes.Geometry;

namespace GPMVehicleControlSystem.Models.VehicleControl.VehicleComponent
{
    public class clsNavigation : CarComponent
    {
        public enum AGV_DIRECTION : ushort
        {
            FORWARD, LEFT, RIGHT, STOP
        }
        public override COMPOENT_NAME component_name => COMPOENT_NAME.NAVIGATION;

        public new NavigationState Data => StateData == null ? new NavigationState()
        {
            lastVisitedNode= new RosSharp.RosBridgeClient.MessageTypes.Std.Int32(17),
        } : (NavigationState)StateData;

        public event EventHandler<AGV_DIRECTION> OnDirectionChanged;
        public event EventHandler<int> OnTagReach;

        public double LinearSpeed { get; private set; } = 0;
        public double AngularSpeed { get; private set; } = 0;

        private int _previousTag = 0;
        private Point last_position { get; set; } = new Point();
        private double last_theta { get; set; } = 0;

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
            LastVisitedTag = Data.lastVisitedNode.data;
            LinearSpeed = CalculateLinearSpeed(Data.robotPose.pose.position);
            AngularSpeed = CalculateAngularSpeed(Angle);
            Direction = ConvertToDirection(Data.robotDirect);
            last_position = Data.robotPose.pose.position;
            last_theta = Angle;
            if (Data.errorCode != 0)
            {
                return STATE.ABNORMAL;
            }
            else
            {

            }
            return STATE.NORMAL;
        }
        //180d 3.14  
        private double CalculateAngularSpeed(double angle)
        {
            double angle_diff = angle - last_theta;
            double time_period = (DateTime.Now - lastUpdateTime).TotalSeconds;
            return Math.Round(Math.Abs(angle_diff) / time_period * Math.PI / 180.0, 2);
        }

        private double CalculateLinearSpeed(Point currentPosition)
        {
            double time_period = (DateTime.Now - lastUpdateTime).TotalSeconds;
            //移動距離 (m)
            var displacement = Math.Sqrt(Math.Pow((currentPosition.x - last_position.x), 2) + Math.Pow((currentPosition.y - last_position.y), 2)); //m
            return Math.Round(displacement / time_period, 1);
        }
    }
}
