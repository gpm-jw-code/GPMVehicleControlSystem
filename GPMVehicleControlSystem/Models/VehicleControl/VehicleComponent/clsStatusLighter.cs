using AGVSystemCommonNet6.Abstracts;
using GPMVehicleControlSystem.VehicleControl.DIOModule;
using static GPMVehicleControlSystem.VehicleControl.DIOModule.clsDOModule;

namespace GPMVehicleControlSystem.Models.VehicleControl.VehicleComponent
{
    public class clsStatusLighter : Lighter
    {
        public clsStatusLighter(clsDOModule DOModule) : base(DOModule)
        {

        }

        public override void CloseAll()
        {
            DOModule.SetState(DO_ITEM.AGV_DiractionLight_R, false);
            DOModule.SetState(DO_ITEM.AGV_DiractionLight_G, false);
            DOModule.SetState(DO_ITEM.AGV_DiractionLight_B, false);
            DOModule.SetState(DO_ITEM.AGV_DiractionLight_Y, false);
        }

        public override void OpenAll()
        {
            DOModule.SetState(DO_ITEM.AGV_DiractionLight_R, true);
            DOModule.SetState(DO_ITEM.AGV_DiractionLight_G, true);
            DOModule.SetState(DO_ITEM.AGV_DiractionLight_Y, true);
            DOModule.SetState(DO_ITEM.AGV_DiractionLight_B, true);
        }
        public void RUN()
        {
            DOModule.SetState(DO_ITEM.AGV_DiractionLight_R, false);
            DOModule.SetState(DO_ITEM.AGV_DiractionLight_G, true);
            DOModule.SetState(DO_ITEM.AGV_DiractionLight_Y, false);
        }
        public void DOWN()
        {

            DOModule.SetState(DO_ITEM.AGV_DiractionLight_R, true);
            DOModule.SetState(DO_ITEM.AGV_DiractionLight_G, false);
            DOModule.SetState(DO_ITEM.AGV_DiractionLight_Y, false);
        }
        public void IDLE()
        {

            DOModule.SetState(DO_ITEM.AGV_DiractionLight_R, false);
            DOModule.SetState(DO_ITEM.AGV_DiractionLight_G, false);
            DOModule.SetState(DO_ITEM.AGV_DiractionLight_Y, true);
        }
        public void ONLINE()
        {
            DOModule.SetState(DO_ITEM.AGV_DiractionLight_B, true);
        }
        public void OFFLINE()
        {
            DOModule.SetState(DO_ITEM.AGV_DiractionLight_B, false);
        }
    }
}
