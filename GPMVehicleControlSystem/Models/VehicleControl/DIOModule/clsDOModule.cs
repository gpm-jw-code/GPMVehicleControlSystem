using GPMVehicleControlSystem.Tools;
using System.Linq;
using System.Net.Sockets;

namespace GPMVehicleControlSystem.VehicleControl.DIOModule
{
    public class clsDOModule : clsDIModule
    {
        public enum DO_ITEM : byte
        {
            Unknown = 0xFF,
            Recharge_Circuit = 0x08,
            Safety_Relays_Reset = 0x09,
            Horizon_Motor_Stop = 0x0A,
            Horizon_Motor_Free,
            Horizon_Motor_Reset,
            Vertical_Motor_Stop,
            Vertical_Motor_Free,
            Vertical_Motor_Reset,

            Front_LsrBypass,
            Back_LsrBypass,
            Left_LsrBypass,
            Right_LsrBypass,

            AGV_DiractionLight_Front = 0x16,
            AGV_DiractionLight_Back,
            AGV_DiractionLight_R,
            AGV_DiractionLight_Y,
            AGV_DiractionLight_G,
            AGV_DiractionLight_B,
            AGV_DiractionLight_Left,
            AGV_DiractionLight_Right,
            Vertical_Hardware_limit_bypass = 0x1F,

            AGV_VALID = 0x20,
            AGV_AGV_READY = 0x23,
            AGV_TR_REQ = 0x24,
            AGV_BUSY = 0x25,
            AGV_COMPT = 0x26,
            TO_EQ_Low = 0x28,
            TO_EQ_Up = 0x29,
            CMD_reserve_Up = 0x2A,
            CMD_reserve_Low = 0x2B,
            Front_Protection_Sensor_IN_1 = 0x30,
            Front_Protection_Sensor_CIN_1,
            Front_Protection_Sensor_IN_2,
            Front_Protection_Sensor_CIN_2,
            Front_Protection_Sensor_IN_3,
            Front_Protection_Sensor_CIN_3,
            Front_Protection_Sensor_IN_4,
            Front_Protection_Sensor_CIN_4,

            Back_Protection_Sensor_IN_1,
            Back_Protection_Sensor_CIN_1,
            Back_Protection_Sensor_IN_2,
            Back_Protection_Sensor_CIN_2,
            Back_Protection_Sensor_IN_3,
            Back_Protection_Sensor_CIN_3,
            Back_Protection_Sensor_IN_4,
            Back_Protection_Sensor_CIN_4,
        }
        Dictionary<DO_ITEM, int> OUTPUT_INDEXS = new Dictionary<DO_ITEM, int>();

        public List<clsIOSignal> VCSOutputs = new List<clsIOSignal>();
        public clsDOModule(string IP, int Port) : base(IP, Port)
        {
        }
        public clsDOModule() : base()
        {
        }
        protected override void RegistSignalEvents()
        {
        }
        public override void ReadIOSettingsFromIniFile()
        {
            IniHelper iniHelper = new IniHelper(Path.Combine(Environment.CurrentDirectory, "param/IO_Wago.ini"));

            try
            {
                Start = ushort.Parse(iniHelper.GetValue("OUTPUT", "Start"));
                Size = ushort.Parse(iniHelper.GetValue("OUTPUT", "Size"));
                for (ushort i = 0; i < Size; i++)
                {
                    var Address = $"Y{i.ToString("X4")}";
                    var RigisterName = iniHelper.GetValue("OUTPUT", Address);
                    var reg = new clsIOSignal(RigisterName, Address);
                    reg.index = i;
                    reg.State = false;
                    if (RigisterName != "")
                    {

                        var do_item = Enum.GetValues(typeof(DO_ITEM)).Cast<DO_ITEM>().First(di => di.ToString() == RigisterName);
                        if (do_item != DO_ITEM.Unknown)
                        {
                            OUTPUT_INDEXS.Add(do_item, i);
                        }
                    }

                    VCSOutputs.Add(reg);
                }
            }
            catch (Exception ex)
            {

            }
        }

        public async void ResetMotor(object? sender, EventArgs e)
        {
            await ResetMotor();
        }
        public async Task ResetMotor()
        {
            Console.WriteLine("Reset Motor Process Start");
            SetState(DO_ITEM.Horizon_Motor_Stop, true);

            //安全迴路RELAY
            SetState(DO_ITEM.Safety_Relays_Reset, true);
            await Task.Delay(200);
            SetState(DO_ITEM.Safety_Relays_Reset, false);

            SetState(DO_ITEM.Horizon_Motor_Stop, false);
            SetState(DO_ITEM.Horizon_Motor_Reset, true);
            await Task.Delay(200);
            SetState(DO_ITEM.Horizon_Motor_Reset, false);
            Console.WriteLine("Reset Motor Process End");

        }

        public void SetState(string address, bool state)
        {
            if (!IsConnected())
                Connect();
            clsIOSignal? DO = VCSOutputs.FirstOrDefault(k => k.Address == address + "");
            DO.State = state;
            master?.WriteSingleCoil((ushort)(Start + DO.index), DO.State);

        }

        public void SetState(DO_ITEM signal, bool state)
        {
            try
            {
                if (!IsConnected())
                    Connect();
                clsIOSignal? DO = VCSOutputs.FirstOrDefault(k => k.Name == signal + "");
                if (DO != null)
                {
                    DO.State = state;
                    master?.WriteSingleCoil((ushort)(Start + DO.index), DO.State);
                }

            }
            catch (Exception)
            {
                master = null;
                SetState(signal, state);
            }

        }
        public new bool GetState(DO_ITEM signal)
        {
            return VCSOutputs.FirstOrDefault(k => k.Name == signal + "").State;
        }
        public void ResetHandshakeSignals()
        {
            SetState(DO_ITEM.AGV_COMPT, false);
            SetState(DO_ITEM.AGV_BUSY, false);
            SetState(DO_ITEM.AGV_AGV_READY, false);
            SetState(DO_ITEM.AGV_TR_REQ, false);
            SetState(DO_ITEM.AGV_VALID, false);
        }
        public override async void StartAsync()
        {
            if (!IsConnected())
                Connect();

            //await Task.Run(async () =>
            //{
            //    while (true)
            //    {
            //        await Task.Delay(1);
            //        if (!IsConnected())
            //        {
            //            Connect();
            //            continue;
            //        }
            //        PauseSignal.WaitOne();
            //        try
            //        {
            //            master?.WriteMultipleCoils(Start, VCSOutputs.Select(si => si.State).ToArray());
            //        }
            //        catch (Exception ex)
            //        {
            //            Console.WriteLine(ex.Message);
            //        }
            //    }
            //});
        }

    }
}
