using GPMRosMessageNet.Messages;
using GPMVehicleControlSystem.Models.VehicleControl.VehicleComponent.Abstracts;

namespace GPMVehicleControlSystem.Models.VehicleControl.VehicleComponent
{
    public class clsNavigation : CarComponent
    {
        public override COMPOENT_NAME component_name => COMPOENT_NAME.NAVIGATION;

        public new NavigationState Data => (NavigationState)base.StateData;

        public override STATE CheckStateDataContent()
        {
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
