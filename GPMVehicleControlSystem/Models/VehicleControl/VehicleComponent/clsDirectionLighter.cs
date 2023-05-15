using GPMVehicleControlSystem.Models.Abstracts;
using GPMVehicleControlSystem.Models.VehicleControl.DIOModule;
using static GPMVehicleControlSystem.Models.VehicleControl.DIOModule.clsDOModule;

namespace GPMVehicleControlSystem.Models.VehicleControl.VehicleComponent
{
    public class clsDirectionLighter : Lighter
    {
        public clsDOModule DOModule { get; }
        public clsDirectionLighter(clsDOModule DOModule) : base(DOModule)
        {
            this.DOModule = DOModule;
        }

        public override void CloseAll()
        {
            AbortFlash();
            this.DOModule.SetState(DO_ITEM.AGV_DiractionLight_Front, false);
            this.DOModule.SetState(DO_ITEM.AGV_DiractionLight_Back, false);
            this.DOModule.SetState(DO_ITEM.AGV_DiractionLight_Right, false);
            this.DOModule.SetState(DO_ITEM.AGV_DiractionLight_Left, false);
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

            this.DOModule.SetState(DO_ITEM.AGV_DiractionLight_Front, false);
            this.DOModule.SetState(DO_ITEM.AGV_DiractionLight_Back, false);
            this.DOModule.SetState(DO_ITEM.AGV_DiractionLight_Left, false);
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

            this.DOModule.SetState(DO_ITEM.AGV_DiractionLight_Front, false);
            this.DOModule.SetState(DO_ITEM.AGV_DiractionLight_Back, false);
            this.DOModule.SetState(DO_ITEM.AGV_DiractionLight_Right, false);
            if (opened)
                Flash(DO_ITEM.AGV_DiractionLight_Left);
            else
            {
                AbortFlash();
                this.DOModule.SetState(DO_ITEM.AGV_DiractionLight_Left, false);
            }
        }

        public void Forward(bool opened = true)
        {

            this.DOModule.SetState(DO_ITEM.AGV_DiractionLight_Front, opened);
            this.DOModule.SetState(DO_ITEM.AGV_DiractionLight_Back, false);
            this.DOModule.SetState(DO_ITEM.AGV_DiractionLight_Right, false);
            this.DOModule.SetState(DO_ITEM.AGV_DiractionLight_Left, false);
        }
        public void Backward(bool opened = true)
        {

            this.DOModule.SetState(DO_ITEM.AGV_DiractionLight_Front, false);
            this.DOModule.SetState(DO_ITEM.AGV_DiractionLight_Back, opened);
            this.DOModule.SetState(DO_ITEM.AGV_DiractionLight_Right, false);
            this.DOModule.SetState(DO_ITEM.AGV_DiractionLight_Left, false);
        }

        /// <summary>
        /// 左右燈同時閃
        /// </summary>
        public void Emergency()
        {

        }


    }
}
