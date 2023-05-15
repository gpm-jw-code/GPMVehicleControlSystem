using GPMVehicleControlSystem.Models.Abstracts;
using GPMVehicleControlSystem.Models.VehicleControl.DIOModule;

namespace GPMVehicleControlSystem.Models.VehicleControl.VehicleComponent
{
    public class clsLaser : IDIOUsagable
    {
        public enum LASER_MODE
        {
            Bypass = 0,
            Move = 1,
            Secondary = 2,
            Spin = 5,
            Loading = 7,
            Special = 10,
        }

        public clsDOModule DOModule { get; set; }
        public clsDIModule DIModule { get; set; }

        public clsLaser(clsDOModule DOModule, clsDIModule DIModule)
        {
            this.DOModule = DOModule;
            this.DIModule = DIModule;
        }

        public void ModeSwitch(int mode_int)
        {
            bool[] mode_bools = new bool[4];
            for (int i = 0; i < 4; i++)
            {
                mode_bools[i] = ((mode_int >> i) & 1) != 1;
            }
            DOModule.PauseSignal.Reset();
            Thread.Sleep(100);
            DOModule.SetState(clsDOModule.DO_ITEM.Front_Protection_Sensor_IN_1, mode_bools[0]);
            DOModule.SetState(clsDOModule.DO_ITEM.Back_Protection_Sensor_IN_1, mode_bools[0]);

            DOModule.SetState(clsDOModule.DO_ITEM.Front_Protection_Sensor_IN_2, mode_bools[1]);
            DOModule.SetState(clsDOModule.DO_ITEM.Back_Protection_Sensor_IN_2, mode_bools[1]);

            DOModule.SetState(clsDOModule.DO_ITEM.Front_Protection_Sensor_IN_3, mode_bools[2]);
            DOModule.SetState(clsDOModule.DO_ITEM.Back_Protection_Sensor_IN_3, mode_bools[2]);

            DOModule.SetState(clsDOModule.DO_ITEM.Front_Protection_Sensor_IN_4, mode_bools[3]);
            DOModule.SetState(clsDOModule.DO_ITEM.Back_Protection_Sensor_IN_4, mode_bools[3]);

            DOModule.SetState(clsDOModule.DO_ITEM.Front_Protection_Sensor_CIN_1, !mode_bools[0]);
            DOModule.SetState(clsDOModule.DO_ITEM.Back_Protection_Sensor_CIN_1, !mode_bools[0]);

            DOModule.SetState(clsDOModule.DO_ITEM.Front_Protection_Sensor_CIN_2, !mode_bools[1]);
            DOModule.SetState(clsDOModule.DO_ITEM.Back_Protection_Sensor_CIN_2, !mode_bools[1]);

            DOModule.SetState(clsDOModule.DO_ITEM.Front_Protection_Sensor_CIN_3, !mode_bools[2]);
            DOModule.SetState(clsDOModule.DO_ITEM.Back_Protection_Sensor_CIN_3, !mode_bools[2]);

            DOModule.SetState(clsDOModule.DO_ITEM.Front_Protection_Sensor_CIN_4, !mode_bools[3]);
            DOModule.SetState(clsDOModule.DO_ITEM.Back_Protection_Sensor_CIN_4, !mode_bools[3]);
            DOModule.PauseSignal.Set();

        }
        private LASER_MODE _Mode = LASER_MODE.Bypass;
        public LASER_MODE Mode
        {
            get => _Mode;
            set => ModeSwitch((int)value);
        }

        public bool FrontLaserBypass
        {
            get => DOModule.GetState(clsDOModule.DO_ITEM.Front_LsrBypass);
            set => DOModule.SetState(clsDOModule.DO_ITEM.Front_LsrBypass, value);
        }

        public bool BackLaserBypass
        {
            get => DOModule.GetState(clsDOModule.DO_ITEM.Back_LsrBypass);
            set => DOModule.SetState(clsDOModule.DO_ITEM.Back_LsrBypass, value);
        }

        public bool RightLaserBypass
        {
            get => DOModule.GetState(clsDOModule.DO_ITEM.Right_LsrBypass);
            set => DOModule.SetState(clsDOModule.DO_ITEM.Right_LsrBypass, value);
        }

        public bool LeftLaserBypass
        {
            get => DOModule.GetState(clsDOModule.DO_ITEM.Left_LsrBypass);
            set => DOModule.SetState(clsDOModule.DO_ITEM.Left_LsrBypass, value);
        }
    }
}
