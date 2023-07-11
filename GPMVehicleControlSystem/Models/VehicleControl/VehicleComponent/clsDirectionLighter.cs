using AGVSystemCommonNet6.Abstracts;
using GPMVehicleControlSystem.VehicleControl.DIOModule;
using static GPMVehicleControlSystem.VehicleControl.DIOModule.clsDOModule;

namespace GPMVehicleControlSystem.Models.VehicleControl.VehicleComponent
{
    public class clsDirectionLighter : Lighter
    {
        public clsDOModule DOModule { get; }
        public clsDirectionLighter(clsDOModule DOModule) : base(DOModule)
        {
            this.DOModule = DOModule;
        }

        public override async void CloseAll()
        {
            try
            {
                AbortFlash();
                this.DOModule.SetState(DO_ITEM.AGV_DiractionLight_Front, false);
                this.DOModule.SetState(DO_ITEM.AGV_DiractionLight_Back, false);
                this.DOModule.SetState(DO_ITEM.AGV_DiractionLight_Right, false);
                this.DOModule.SetState(DO_ITEM.AGV_DiractionLight_Left, false);
            }
            catch (Exception ex)
            {
            }
        }

        public override void OpenAll()
        {
            this.DOModule.SetState(DO_ITEM.AGV_DiractionLight_Front, true);
            this.DOModule.SetState(DO_ITEM.AGV_DiractionLight_Back, true);
            this.DOModule.SetState(DO_ITEM.AGV_DiractionLight_Right, true);
            this.DOModule.SetState(DO_ITEM.AGV_DiractionLight_Left, true);
        }

        public void TurnRight(bool opened = true)
        {
            CloseAll();
            this.DOModule.SetState(DO_ITEM.AGV_DiractionLight_Front, false);
            this.DOModule.SetState(DO_ITEM.AGV_DiractionLight_Back, false);
            this.DOModule.SetState(DO_ITEM.AGV_DiractionLight_Left, false);
            this.DOModule.SetState(DO_ITEM.AGV_DiractionLight_Right, true);
            if (opened)
                Flash(DO_ITEM.AGV_DiractionLight_Right);
            else
            {
                AbortFlash();
                this.DOModule.SetState(DO_ITEM.AGV_DiractionLight_Right, false);
            }
        }
        public void TurnLeft(bool opened = true)
        {
            CloseAll();
            this.DOModule.SetState(DO_ITEM.AGV_DiractionLight_Front, false);
            this.DOModule.SetState(DO_ITEM.AGV_DiractionLight_Back, false);
            this.DOModule.SetState(DO_ITEM.AGV_DiractionLight_Right, false);
            this.DOModule.SetState(DO_ITEM.AGV_DiractionLight_Left, true);
            if (opened)
                Flash(DO_ITEM.AGV_DiractionLight_Left);
            else
            {
                AbortFlash();
                this.DOModule.SetState(DO_ITEM.AGV_DiractionLight_Left, false);
            }
        }

        public async void Forward(bool opened = true)
        {
            await Task.Delay(500);
            this.DOModule.SetState(DO_ITEM.AGV_DiractionLight_Front, opened);
            this.DOModule.SetState(DO_ITEM.AGV_DiractionLight_Back, false);
            this.DOModule.SetState(DO_ITEM.AGV_DiractionLight_Right, false);
            this.DOModule.SetState(DO_ITEM.AGV_DiractionLight_Left, false);
        }
        public async void Backward(bool opened = true,int delay = 500)
        {
            await Task.Delay(delay);
            this.DOModule.SetState(DO_ITEM.AGV_DiractionLight_Front, false);
            this.DOModule.SetState(DO_ITEM.AGV_DiractionLight_Back, opened);
            this.DOModule.SetState(DO_ITEM.AGV_DiractionLight_Right, false);
            this.DOModule.SetState(DO_ITEM.AGV_DiractionLight_Left, false);
        }

        internal void WaitPassLights()
        {
            Flash(new DO_ITEM[] { DO_ITEM.AGV_DiractionLight_Right, DO_ITEM.AGV_DiractionLight_Left }, 200);
        }

        internal void LightSwitchByAGVDirection(object? sender, clsNavigation.AGV_DIRECTION e)
        {
            CloseAll();

            if (e == clsNavigation.AGV_DIRECTION.FORWARD)
                Forward();
            else if (e == clsNavigation.AGV_DIRECTION.RIGHT)
                TurnRight();
            else if (e == clsNavigation.AGV_DIRECTION.LEFT)
                TurnLeft();
            else if (e == clsNavigation.AGV_DIRECTION.STOP)
                CloseAll();

        }

    }
}
