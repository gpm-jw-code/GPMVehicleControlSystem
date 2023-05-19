
using AGVSystemCommonNet6.Abstracts;
using AGVSystemCommonNet6.Log;
using GPMVehicleControlSystem.VehicleControl.DIOModule;

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
            Move_Short = 11,
            Spin_Shor = 12

        }

        /// <summary>
        /// AGVS站點雷射預設定植
        /// </summary>
        public enum AGVS_LASER_SETTING_ORDER
        {
            BYPASS,
            NORMAL,
        }

        public clsDOModule DOModule { get; set; }
        public clsDIModule DIModule { get; set; }
        private AGVS_LASER_SETTING_ORDER _AgvsLsrSetting = AGVS_LASER_SETTING_ORDER.NORMAL;
        public AGVS_LASER_SETTING_ORDER AgvsLsrSetting
        {
            get => _AgvsLsrSetting;
            set
            {
                if (_AgvsLsrSetting != value)
                {
                    _AgvsLsrSetting = value;
                    if (value == AGVS_LASER_SETTING_ORDER.BYPASS)
                        Mode = LASER_MODE.Bypass;
                }
            }
        }
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
            Thread.Sleep(400);
            DOModule.SetState(clsDOModule.DO_ITEM.Front_Protection_Sensor_IN_1, mode_bools[0]);
            DOModule.SetState(clsDOModule.DO_ITEM.Front_Protection_Sensor_CIN_1, !mode_bools[0]);

            DOModule.SetState(clsDOModule.DO_ITEM.Front_Protection_Sensor_IN_2, mode_bools[1]);
            DOModule.SetState(clsDOModule.DO_ITEM.Front_Protection_Sensor_CIN_2, !mode_bools[1]);

            DOModule.SetState(clsDOModule.DO_ITEM.Front_Protection_Sensor_IN_3, mode_bools[2]);
            DOModule.SetState(clsDOModule.DO_ITEM.Front_Protection_Sensor_CIN_3, !mode_bools[2]);

            DOModule.SetState(clsDOModule.DO_ITEM.Front_Protection_Sensor_IN_4, mode_bools[3]);
            DOModule.SetState(clsDOModule.DO_ITEM.Front_Protection_Sensor_CIN_4, !mode_bools[3]);

            DOModule.SetState(clsDOModule.DO_ITEM.Back_Protection_Sensor_CIN_1, !mode_bools[0]);
            DOModule.SetState(clsDOModule.DO_ITEM.Back_Protection_Sensor_IN_1, mode_bools[0]);

            DOModule.SetState(clsDOModule.DO_ITEM.Back_Protection_Sensor_IN_2, mode_bools[1]);
            DOModule.SetState(clsDOModule.DO_ITEM.Back_Protection_Sensor_CIN_2, !mode_bools[1]);

            DOModule.SetState(clsDOModule.DO_ITEM.Back_Protection_Sensor_IN_3, mode_bools[2]);
            DOModule.SetState(clsDOModule.DO_ITEM.Back_Protection_Sensor_CIN_3, !mode_bools[2]);

            DOModule.SetState(clsDOModule.DO_ITEM.Back_Protection_Sensor_IN_4, mode_bools[3]);
            DOModule.SetState(clsDOModule.DO_ITEM.Back_Protection_Sensor_CIN_4, !mode_bools[3]);
            DOModule.PauseSignal.Set();

            LOG.TRACE($"Laser Mode Chaged To : {mode_int}({_Mode})");
        }

        internal void LaserChangeByAGVDirection(object? sender, clsNavigation.AGV_DIRECTION direction)
        {
            if (AgvsLsrSetting == AGVS_LASER_SETTING_ORDER.BYPASS)
            {
                Mode = LASER_MODE.Bypass;
                LOG.INFO($"雷射設定組 = {LASER_MODE.Bypass} (因派車系統點位雷射設定為 0)");
            }
            else
            {
                if (_Mode == LASER_MODE.Loading)
                    return;

                if (direction == clsNavigation.AGV_DIRECTION.FORWARD)
                    Mode = LASER_MODE.Move;
                else if (direction == clsNavigation.AGV_DIRECTION.LEFT | direction == clsNavigation.AGV_DIRECTION.RIGHT)
                    Mode = LASER_MODE.Spin;
            }
        }

        private LASER_MODE _Mode = LASER_MODE.Bypass;
        public LASER_MODE Mode
        {
            get => _Mode;
            set
            {
                _Mode = value;
                if (value == LASER_MODE.Bypass)
                {
                    FrontLaserBypass = BackLaserBypass = RightLaserBypass = LeftLaserBypass = true;
                }
                else if (value == LASER_MODE.Loading)
                {
                    FrontLaserBypass = BackLaserBypass = false;
                    LeftLaserBypass = RightLaserBypass = true;
                }
                else
                {
                    FrontLaserBypass = BackLaserBypass = LeftLaserBypass = RightLaserBypass = false;
                }

                ModeSwitch((int)value);
            }
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

        /// <summary>
        /// 前後左右雷射Bypass全部關閉
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        internal void AllLaserActive()
        {
            FrontLaserBypass = BackLaserBypass = RightLaserBypass = LeftLaserBypass = false;
        }


        /// <summary>
        /// 前後左右雷射Bypass全部開啟
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        internal void AllLaserDisable()
        {
            FrontLaserBypass = BackLaserBypass = RightLaserBypass = LeftLaserBypass = true;
        }
    }
}
