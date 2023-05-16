using GPMRosMessageNet.Messages;
using GPMVehicleControlSystem.Models.VehicleControl.VehicleComponent.Abstracts;

namespace GPMVehicleControlSystem.Models.VehicleControl.VehicleComponent
{
    public class clsNavigation : CarComponent
    {
        public enum AGV_DIRECTION : ushort
        {
            FORWARD, LEFT, RIGHT, STOP
        }
        public override COMPOENT_NAME component_name => COMPOENT_NAME.NAVIGATION;

        public new NavigationState Data => (NavigationState)base.StateData;

        public event EventHandler<AGV_DIRECTION> OnDirectionChanged;
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
