using AGVSystemCommonNet6.Abstracts;
using GPMVehicleControlSystem.Tools;
using Modbus.Device;
using System.Net.Sockets;

namespace GPMVehicleControlSystem.VehicleControl.DIOModule
{
    public class clsDIModule : Connection
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



        /// <summary>
        /// EMO按壓
        /// </summary>
        public event EventHandler OnEMO;

        public event EventHandler OnResetButtonPressed;


        /// <summary>
        /// 前方遠處雷射觸發
        /// </summary>
        public event EventHandler OnFrontArea1LaserTrigger;
        /// <summary>
        /// 後方遠處雷射觸發
        /// </summary>
        public event EventHandler OnBackArea1LaserTrigger;


        /// <summary>
        /// 前方遠處雷射觸發恢復
        /// </summary>
        public event EventHandler OnFrontArea1LaserRecovery;
        /// <summary>
        /// 後方遠處雷射觸發恢復
        /// </summary>
        public event EventHandler OnBackArea1LaserRecovery;




        /// <summary>
        /// 前方遠處雷射觸發
        /// </summary>
        public event EventHandler OnFrontArea2LaserTrigger;
        /// <summary>
        /// 後方遠處雷射觸發
        /// </summary>
        public event EventHandler OnBackArea2LaserTrigger;


        /// <summary>
        /// 前方遠處雷射觸發恢復
        /// </summary>
        public event EventHandler OnFrontArea2LaserRecovery;
        /// <summary>
        /// 後方遠處雷射觸發恢復
        /// </summary>
        public event EventHandler OnBackArea2LaserRecovery;


        /// <summary>
        /// 前方近處雷射觸發
        /// </summary>
        public event EventHandler OnFrontNearAreaLaserTrigger;
        /// <summary>
        /// 後方近處雷射觸發
        /// </summary>
        public event EventHandler OnBackNearAreaLaserTrigger;




        /// <summary>
        /// 前方近處處雷射觸發恢復
        /// </summary>
        public event EventHandler OnFrontNearAreaLaserRecovery;
        /// <summary>
        /// 後方遠處雷射觸發恢復
        /// </summary>
        public event EventHandler OnBackNearAreaLaserRecovery;
        public event EventHandler OnFrontSecondObstacleSensorDetected;
        public Action OnResetButtonPressing { get; set; }

        public Action OnHS_EQ_READY { get; internal set; }

        Dictionary<DI_ITEM, int> INPUT_INDEXS = new Dictionary<DI_ITEM, int>();

        public List<clsIOSignal> VCSInputs = new List<clsIOSignal>();
        public ushort Start { get; set; }
        public ushort Size { get; set; }

        public clsDIModule()
        {
            ReadIOSettingsFromIniFile();
            RegistSignalEvents();
        }
        public clsDIModule(string IP, int Port) : base(IP, Port)
        {
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
            VCSInputs[INPUT_INDEXS[DI_ITEM.Panel_Reset_PB]].OnSignalON += (s, e) => OnResetButtonPressed?.Invoke(s, e);

            VCSInputs[INPUT_INDEXS[DI_ITEM.FrontProtection_Area_Sensor_1]].OnSignalOFF += (s, e) => OnFrontArea1LaserTrigger?.Invoke(s, e);
            VCSInputs[INPUT_INDEXS[DI_ITEM.FrontProtection_Area_Sensor_2]].OnSignalOFF += (s, e) => OnFrontArea2LaserTrigger?.Invoke(s, e);
            VCSInputs[INPUT_INDEXS[DI_ITEM.BackProtection_Area_Sensor_1]].OnSignalOFF += (s, e) => OnBackArea1LaserTrigger?.Invoke(s, e);
            VCSInputs[INPUT_INDEXS[DI_ITEM.BackProtection_Area_Sensor_2]].OnSignalOFF += (s, e) => OnBackArea2LaserTrigger?.Invoke(s, e);

            VCSInputs[INPUT_INDEXS[DI_ITEM.FrontProtection_Area_Sensor_1]].OnSignalON += (s, e) => OnFrontArea1LaserRecovery?.Invoke(s, e);
            VCSInputs[INPUT_INDEXS[DI_ITEM.FrontProtection_Area_Sensor_2]].OnSignalON += (s, e) => OnFrontArea2LaserRecovery?.Invoke(s, e);
            VCSInputs[INPUT_INDEXS[DI_ITEM.BackProtection_Area_Sensor_1]].OnSignalON += (s, e) => OnBackArea1LaserRecovery?.Invoke(s, e);
            VCSInputs[INPUT_INDEXS[DI_ITEM.BackProtection_Area_Sensor_2]].OnSignalON += (s, e) => OnBackArea2LaserRecovery?.Invoke(s, e);

            VCSInputs[INPUT_INDEXS[DI_ITEM.FrontProtection_Area_Sensor_3]].OnSignalON += (s, e) => OnFrontNearAreaLaserRecovery?.Invoke(s, e);
            VCSInputs[INPUT_INDEXS[DI_ITEM.BackProtection_Area_Sensor_3]].OnSignalON += (s, e) => OnBackNearAreaLaserRecovery?.Invoke(s, e);
            VCSInputs[INPUT_INDEXS[DI_ITEM.FrontProtection_Area_Sensor_3]].OnSignalOFF += (s, e) => OnFrontNearAreaLaserTrigger?.Invoke(s, e);
            VCSInputs[INPUT_INDEXS[DI_ITEM.BackProtection_Area_Sensor_3]].OnSignalOFF += (s, e) => OnBackNearAreaLaserTrigger?.Invoke(s, e);

            VCSInputs[INPUT_INDEXS[DI_ITEM.FrontProtection_Obstacle_Sensor]].OnSignalOFF += (s, e) => OnFrontSecondObstacleSensorDetected?.Invoke(s, e);

            VCSInputs[INPUT_INDEXS[DI_ITEM.Panel_Reset_PB]].OnSignalON += (s, e) => OnResetButtonPressing();

            VCSInputs[INPUT_INDEXS[DI_ITEM.EQ_READY]].OnSignalON += (s, e) => OnResetButtonPressing();


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
