using AGVSystemCommonNet6.Abstracts;
using AGVSystemCommonNet6.Log;
using GPMVehicleControlSystem.Tools;
using Modbus.Device;
using System.Net.Sockets;
using static GPMVehicleControlSystem.Models.VehicleControl.AGVControl.CarController;

namespace GPMVehicleControlSystem.VehicleControl.DIOModule
{
    public partial class clsDIModule : Connection
    {
        public enum DI_ITEM : byte
        {
            Unknown = 0xff,
            EMO = 0x08,
            Bumper_Sensor = 0X0A,
            Panel_Reset_PB,
            Horizon_Motor_Switch,
            LeftProtection_Area_Sensor_2,
            RightProtection_Area_Sensor_2,
            Fork_Sensor_1,
            Fork_Under_Pressing_Sensor,
            Horizon_Motor_Error_1 = 0x14,
            Horizon_Motor_Error_2 = 0x16,
            Vertical_Motor_Error = 0x18,
            Vertical_Home_Pos = 0x1B,
            Vertical_Up_Hardware_limit = 0x1E,
            Vertical_Down_Hardware_limit = 0x1F,
            EQ_L_REQ = 0x20,
            EQ_U_REQ = 0x21,
            EQ_READY = 0x23,
            EQ_UP_READY = 0x24,
            EQ_LOW_READY = 0x25,
            EQ_BUSY = 0x26,
            EQ_GO = 0x28,
            Cst_Sensor_1 = 0X2D,
            Cst_Sensor_2 = 0x2E,
            FrontProtection_Obstacle_Sensor = 0x2F,
            FrontProtection_Area_Sensor_1 = 0x30,
            FrontProtection_Area_Sensor_2,
            FrontProtection_Area_Sensor_3,
            FrontProtection_Area_Sensor_4,
            BackProtection_Area_Sensor_1,
            BackProtection_Area_Sensor_2,
            BackProtection_Area_Sensor_3,
            BackProtection_Area_Sensor_4,
        }
        protected ModbusIpMaster? master;

        public ManualResetEvent PauseSignal = new ManualResetEvent(true);

        bool isFrontLaserA1Trigger => !GetState(DI_ITEM.FrontProtection_Area_Sensor_1);
        bool isFrontLaserA2Trigger => !GetState(DI_ITEM.FrontProtection_Area_Sensor_2);
        bool isFrontLaserA3Trigger => !GetState(DI_ITEM.FrontProtection_Area_Sensor_3);
        bool isFrontLaserA4Trigger => !GetState(DI_ITEM.FrontProtection_Area_Sensor_4);

        bool isBackLaserA1Trigger => !GetState(DI_ITEM.BackProtection_Area_Sensor_1);
        bool isBackLaserA2Trigger => !GetState(DI_ITEM.BackProtection_Area_Sensor_2);
        bool isBackLaserA3Trigger => !GetState(DI_ITEM.BackProtection_Area_Sensor_3);
        bool isBackLaserA4Trigger => !GetState(DI_ITEM.BackProtection_Area_Sensor_4);

        bool isRightLaserTrigger => !GetState(DI_ITEM.RightProtection_Area_Sensor_2);
        bool isLeftLaserTrigger => !GetState(DI_ITEM.LeftProtection_Area_Sensor_2);


        public event EventHandler<ROBOT_CONTROL_CMD> OnLaserDIRecovery;
        public event EventHandler OnFarLaserDITrigger;
        public event EventHandler OnNearLaserDiTrigger;

        /// <summary>
        /// EMO按壓
        /// </summary>
        public event EventHandler OnEMO;

        /// <summary>
        /// Bump Sensor觸發
        /// </summary>
        public event EventHandler OnBumpSensorPressed;

        public event EventHandler OnResetButtonPressed;


        public event EventHandler OnFrontSecondObstacleSensorDetected;
        public Action OnResetButtonPressing { get; set; }

        public Action OnHS_EQ_READY { get; internal set; }

        Dictionary<DI_ITEM, int> INPUT_INDEXS = new Dictionary<DI_ITEM, int>();

        public List<clsIOSignal> VCSInputs = new List<clsIOSignal>();
        public ushort Start { get; set; }
        public ushort Size { get; set; }

        public clsDIModule()
        {

        }
        public clsDIModule(string IP, int Port)
        {
            this.IP = IP;
            this.Port = Port;
            ReadIOSettingsFromIniFile();
            RegistSignalEvents();
        }
        public clsDIModule(string IP, int Port, clsDOModule DoModuleRef)
        {
            this.IP = IP;
            this.Port = Port;
            this.DoModuleRef = DoModuleRef;
            ReadIOSettingsFromIniFile();
            RegistSignalEvents();
        }

        virtual public void ReadIOSettingsFromIniFile()
        {
            IniHelper iniHelper = new IniHelper(Path.Combine(Environment.CurrentDirectory, "param/IO_Wago.ini"));
            try
            {
                Start = ushort.Parse(iniHelper.GetValue("INPUT", "Start"));
                Size = ushort.Parse(iniHelper.GetValue("INPUT", "Size"));
                for (ushort i = 0; i < Size; i++)
                {
                    var Address = $"X{i.ToString("X4")}";
                    var RigisterName = iniHelper.GetValue("INPUT", Address);
                    var reg = new clsIOSignal(RigisterName, Address);
                    if (RigisterName != "")
                    {
                        var di_item = Enum.GetValues(typeof(DI_ITEM)).Cast<DI_ITEM>().First(di => di.ToString() == RigisterName);
                        if (di_item != DI_ITEM.Unknown)
                        {
                            INPUT_INDEXS.Add(di_item, i);
                        }
                    }
                    VCSInputs.Add(reg);
                }
            }
            catch (Exception ex)
            {

            }

        }
        public override bool Connect()
        {
            try
            {
                var client = new TcpClient(IP, Port);
                master = ModbusIpMaster.CreateIp(client);
                master.Transport.ReadTimeout = 5000;
                master.Transport.WriteTimeout = 5000;
                master.Transport.Retries = 10;
                Console.WriteLine("Wago DI/O Module Modbus TCP Connected!");
                return true;
            }
            catch (Exception ex)
            {
                master = null;
                return false;
            }
        }

        public override void Disconnect()
        {
            master?.Dispose();
        }

        public override bool IsConnected()
        {
            return master != null;
        }

        internal void SetState(string address, bool state)
        {
            VCSInputs.FirstOrDefault(k => k.Address == address).State = state;
        }

        public bool GetState(DI_ITEM signal)
        {
            return VCSInputs.FirstOrDefault(k => k.Name == signal + "").State;
        }
        protected virtual void RegistSignalEvents()
        {
            VCSInputs[INPUT_INDEXS[DI_ITEM.EMO]].OnSignalOFF += (s, e) => OnEMO?.Invoke(s, e);
            VCSInputs[INPUT_INDEXS[DI_ITEM.Bumper_Sensor]].OnSignalOFF += (s, e) => OnBumpSensorPressed?.Invoke(s, e);
            VCSInputs[INPUT_INDEXS[DI_ITEM.Panel_Reset_PB]].OnSignalON += (s, e) => OnResetButtonPressed?.Invoke(s, e);

            VCSInputs[INPUT_INDEXS[DI_ITEM.RightProtection_Area_Sensor_2]].OnSignalOFF += NearLaserDiTriggerHandle; ;
            VCSInputs[INPUT_INDEXS[DI_ITEM.LeftProtection_Area_Sensor_2]].OnSignalOFF += NearLaserDiTriggerHandle; ;

            VCSInputs[INPUT_INDEXS[DI_ITEM.RightProtection_Area_Sensor_2]].OnSignalON += LaserRecoveryHandle;
            VCSInputs[INPUT_INDEXS[DI_ITEM.LeftProtection_Area_Sensor_2]].OnSignalON += LaserRecoveryHandle;

            VCSInputs[INPUT_INDEXS[DI_ITEM.FrontProtection_Area_Sensor_1]].OnSignalOFF += FarLsrTriggerHandle;
            VCSInputs[INPUT_INDEXS[DI_ITEM.FrontProtection_Area_Sensor_2]].OnSignalOFF += NearLaserDiTriggerHandle;
            VCSInputs[INPUT_INDEXS[DI_ITEM.FrontProtection_Area_Sensor_3]].OnSignalOFF += NearLaserDiTriggerHandle; ;

            VCSInputs[INPUT_INDEXS[DI_ITEM.BackProtection_Area_Sensor_1]].OnSignalOFF += FarLsrTriggerHandle;
            VCSInputs[INPUT_INDEXS[DI_ITEM.BackProtection_Area_Sensor_2]].OnSignalOFF += NearLaserDiTriggerHandle; ;
            VCSInputs[INPUT_INDEXS[DI_ITEM.BackProtection_Area_Sensor_3]].OnSignalOFF += NearLaserDiTriggerHandle; ;

            VCSInputs[INPUT_INDEXS[DI_ITEM.FrontProtection_Area_Sensor_1]].OnSignalON += LaserRecoveryHandle;
            VCSInputs[INPUT_INDEXS[DI_ITEM.FrontProtection_Area_Sensor_2]].OnSignalON += LaserRecoveryHandle;
            VCSInputs[INPUT_INDEXS[DI_ITEM.FrontProtection_Area_Sensor_3]].OnSignalON += LaserRecoveryHandle;

            VCSInputs[INPUT_INDEXS[DI_ITEM.BackProtection_Area_Sensor_1]].OnSignalON += LaserRecoveryHandle;
            VCSInputs[INPUT_INDEXS[DI_ITEM.BackProtection_Area_Sensor_2]].OnSignalON += LaserRecoveryHandle;
            VCSInputs[INPUT_INDEXS[DI_ITEM.BackProtection_Area_Sensor_3]].OnSignalON += LaserRecoveryHandle;

            VCSInputs[INPUT_INDEXS[DI_ITEM.FrontProtection_Obstacle_Sensor]].OnSignalOFF += (s, e) => OnFrontSecondObstacleSensorDetected?.Invoke(s, e);

        }

        private void NearLaserDiTriggerHandle(object? sender, EventArgs e)
        {
            clsIOSignal laserSignal = sender as clsIOSignal;
            DI_ITEM DI = laserSignal.DI_item;

            if (DI == DI_ITEM.RightProtection_Area_Sensor_2 && IsRightLsrBypass)
                return;

            if (DI == DI_ITEM.LeftProtection_Area_Sensor_2 && IsLeftLsrBypass)
                return;

            if ((DI == DI_ITEM.FrontProtection_Area_Sensor_2 | DI == DI_ITEM.FrontProtection_Area_Sensor_3) && IsFrontLsrBypass)
                return;

            if ((DI == DI_ITEM.BackProtection_Area_Sensor_2 | DI == DI_ITEM.BackProtection_Area_Sensor_3) && IsBackLsrBypass)
                return;

            OnNearLaserDiTrigger?.Invoke(sender, e);
        }

        private void FarLsrTriggerHandle(object? sender, EventArgs e)
        {
            clsIOSignal laserSignal = sender as clsIOSignal;
            DI_ITEM DI = laserSignal.DI_item;
            if (DI == DI_ITEM.FrontProtection_Area_Sensor_1 && !IsFrontLsrBypass)
                return;

            if (DI == DI_ITEM.BackProtection_Area_Sensor_1 && IsBackLsrBypass)
                return;
            OnFarLaserDITrigger?.Invoke(sender, e);
        }

        private void LaserRecoveryHandle(object? sender, EventArgs e)
        {
            clsIOSignal laserSignal = sender as clsIOSignal;
            DI_ITEM DI = laserSignal.DI_item;
            LOG.INFO($"{DI} Laser Reconvery");
            OnLaserDIRecovery?.Invoke(laserSignal, ROBOT_CONTROL_CMD.NONE);

            if (DI == DI_ITEM.RightProtection_Area_Sensor_2 | DI == DI_ITEM.LeftProtection_Area_Sensor_2) //左右雷射復原
            {

                if (isFrontLaserA1Trigger | isFrontLaserA2Trigger | isFrontLaserA3Trigger | isFrontLaserA4Trigger | isBackLaserA1Trigger | isBackLaserA2Trigger | isBackLaserA3Trigger | isBackLaserA4Trigger | isRightLaserTrigger | isLeftLaserTrigger)
                {
                    LOG.INFO($"{DI} Laser Reconvery But LEFT or RIGHT Laser Not Recovery");
                    return;
                }
            }

            if (DI == DI_ITEM.FrontProtection_Area_Sensor_1 | DI == DI_ITEM.BackProtection_Area_Sensor_1)
            {
                if (isFrontLaserA1Trigger | isBackLaserA1Trigger)
                    return;
            }

            if (DI == DI_ITEM.FrontProtection_Area_Sensor_2 | DI == DI_ITEM.BackProtection_Area_Sensor_2)
            {
                if (isFrontLaserA2Trigger | isBackLaserA2Trigger)
                    return;
                else
                {
                    OnLaserDIRecovery?.Invoke(laserSignal, ROBOT_CONTROL_CMD.DECELERATE);
                    return;
                }
            }

            if (DI == DI_ITEM.FrontProtection_Area_Sensor_3 | DI == DI_ITEM.FrontProtection_Area_Sensor_4 | DI == DI_ITEM.BackProtection_Area_Sensor_3 | DI == DI_ITEM.BackProtection_Area_Sensor_4)
            {
                OnLaserDIRecovery?.Invoke(laserSignal, ROBOT_CONTROL_CMD.STOP);
                return;
            }
            OnLaserDIRecovery?.Invoke(laserSignal, ROBOT_CONTROL_CMD.SPEED_Reconvery);

        }

        public virtual async void StartAsync()
        {
            if (!IsConnected())
                Connect();

            await Task.Run(() =>
            {
                while (true)
                {

                    Thread.Sleep(1);

                    if (!IsConnected())
                    {
                        Connect();
                        continue;
                    }

                    try
                    {
                        PauseSignal.WaitOne();
                        bool[]? input = master?.ReadInputs(1, Start, Size);
                        if (input == null)
                            continue;

                        for (int i = 0; i < input.Length; i++)
                        {
                            VCSInputs[i].State = input[i];
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            });
        }

    }
}
