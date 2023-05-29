
using AGVSystemCommonNet6;
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

        private LASER_MODE _Mode = LASER_MODE.Bypass;
        private int _mode_int;
        private int _AgvsLsrSetting = 1;
        public clsDOModule DOModule { get; set; }
        public clsDIModule DIModule { get; set; }
        public int AgvsLsrSetting
        {
            get => _AgvsLsrSetting;
            set
            {
                if (_AgvsLsrSetting != value)
                {
                    _AgvsLsrSetting = value;
                    Console.WriteLine($"變更雷射預設組[AGVS 設定]");
                }
            }
        }
        public clsLaser(clsDOModule DOModule, clsDIModule DIModule)
        {
            this.DOModule = DOModule;
            this.DIModule = DIModule;
        }


        public LASER_MODE Mode
        {
            get => Enum.GetValues(typeof(LASER_MODE)).Cast<LASER_MODE>().First(mo => (int)mo == _mode_int);
            set
            {
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
                _Mode = value;
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

        internal void ApplyAGVSLaserSetting()
        {
            LOG.TRACE($"雷射組數切換為AGVS Setting={AgvsLsrSetting}");
            ModeSwitch(AgvsLsrSetting);
        }


        internal void LaserChangeByAGVDirection(object? sender, clsNavigation.AGV_DIRECTION direction)
        {
            if (direction == clsNavigation.AGV_DIRECTION.FORWARD)
            {
                ModeSwitch(AgvsLsrSetting);
                LOG.INFO($"雷射設定組 = {AgvsLsrSetting}");
            }
            else // 左.右轉
            {
                if (AgvsLsrSetting == 0)
                {
                    Mode = LASER_MODE.Bypass;
                    return;
                }


                if (direction == clsNavigation.AGV_DIRECTION.FORWARD)
                    Mode = LASER_MODE.Move;
                else if (direction == clsNavigation.AGV_DIRECTION.LEFT | direction == clsNavigation.AGV_DIRECTION.RIGHT)
                    Mode = LASER_MODE.Spin;
            }
        }

        public void ModeSwitch(int mode_int)
        {
            if (_mode_int == mode_int)
                return;

            bool[] lsSet= mode_int.To4Booleans();
            bool IN_1 = lsSet[0];
            bool IN_2 = lsSet[1];
            bool IN_3 = lsSet[2];
            bool IN_4 = lsSet[3];
            DOModule.PauseSignal.Reset();
            DIModule.PauseSignal.Reset();
            Thread.Sleep(500);
            DOModule.SetState(clsDOModule.DO_ITEM.Front_Protection_Sensor_IN_1, IN_1);
            DOModule.SetState(clsDOModule.DO_ITEM.Front_Protection_Sensor_CIN_1, !IN_1);

            DOModule.SetState(clsDOModule.DO_ITEM.Front_Protection_Sensor_IN_2, IN_2);
            DOModule.SetState(clsDOModule.DO_ITEM.Front_Protection_Sensor_CIN_2, !IN_2);

            DOModule.SetState(clsDOModule.DO_ITEM.Front_Protection_Sensor_IN_3, IN_3);
            DOModule.SetState(clsDOModule.DO_ITEM.Front_Protection_Sensor_CIN_3, !IN_3);

            DOModule.SetState(clsDOModule.DO_ITEM.Front_Protection_Sensor_IN_4, IN_4);
            DOModule.SetState(clsDOModule.DO_ITEM.Front_Protection_Sensor_CIN_4, !IN_4);

            DOModule.SetState(clsDOModule.DO_ITEM.Back_Protection_Sensor_IN_1, IN_1);
            DOModule.SetState(clsDOModule.DO_ITEM.Back_Protection_Sensor_CIN_1, !IN_1);

            DOModule.SetState(clsDOModule.DO_ITEM.Back_Protection_Sensor_IN_2, IN_2);
            DOModule.SetState(clsDOModule.DO_ITEM.Back_Protection_Sensor_CIN_2, !IN_2);

            DOModule.SetState(clsDOModule.DO_ITEM.Back_Protection_Sensor_IN_3, IN_3);
            DOModule.SetState(clsDOModule.DO_ITEM.Back_Protection_Sensor_CIN_3, !IN_3);

            DOModule.SetState(clsDOModule.DO_ITEM.Back_Protection_Sensor_IN_4, IN_4);
            DOModule.SetState(clsDOModule.DO_ITEM.Back_Protection_Sensor_CIN_4, !IN_4);
            _mode_int = mode_int;
            DOModule.PauseSignal.Set();
            DIModule.PauseSignal.Set();
            LOG.TRACE($"Laser Mode Chaged To : {mode_int}({Mode})");
        }
    }
}
