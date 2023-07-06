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
            try
            {

                DOModule.SetState(DO_ITEM.AGV_DiractionLight_R, false);
                DOModule.SetState(DO_ITEM.AGV_DiractionLight_G, false);
                DOModule.SetState(DO_ITEM.AGV_DiractionLight_Y, true);
            }
            catch (Exception ex)
            {
            }
        }
        public void ONLINE()
        {
            try
            {
                DOModule.SetState(DO_ITEM.AGV_DiractionLight_B, true);
            }
            catch (Exception ex)
            {
            }
        }
        public void OFFLINE()
        {
            try
            {
                DOModule.SetState(DO_ITEM.AGV_DiractionLight_B, false);
            }
            catch (Exception ex)
            {
            }
        }
        public void INITIALIZE()
        {
            Flash(new DO_ITEM[] {
                 DO_ITEM.AGV_DiractionLight_R,
                  DO_ITEM.AGV_DiractionLight_G,
                  DO_ITEM.AGV_DiractionLight_Y,
            }, 200);
        }
    }
}
