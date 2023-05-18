using GPMVehicleControlSystem.Models.Abstracts;
using Modbus.Device;
using System.Net;
using System.Net.Sockets;
using static GPMVehicleControlSystem.Models.VehicleControl.DIOModule.clsDIModule;

namespace GPMVehicleControlSystem.Models.Emulators
{
    public class WagoEmulator : Connection
    {
        ModbusTcpSlave? slave;

        Dictionary<DI_ITEM, int> INPUT_INDEXS;
        public WagoEmulator()
        {
            INPUT_INDEXS = Enum.GetValues(typeof(DI_ITEM)).Cast<DI_ITEM>().ToDictionary(e => e, e => (int)e);
        }


        public override bool Connect()
        {
            IPAddress iPAddress = IPAddress.Parse("127.0.0.1");
            int port = 502;
            TcpListener tcpListener = new TcpListener(iPAddress, port);
            tcpListener.Start();
            try
            {
                slave = ModbusTcpSlave.CreateTcp(1, tcpListener);
                InitializeInputState();
                slave.ModbusSlaveRequestReceived += Slave_ModbusSlaveRequestReceived;
                Task.Run(() =>
                {
                    slave.ListenAsync().Wait();
                });
                return true;
            }
            catch (Exception ex)
            {
                slave = null;
                return false;
            }
        }
        private void InitializeInputState()
        {
            SetState(DI_ITEM.EMO,true);
            SetState(DI_ITEM.Bumper_Sensor,true);
            SetState(DI_ITEM.Horizon_Motor_Switch,true);
         
            SetState(DI_ITEM.FrontProtection_Area_Sensor_1,true);
            SetState(DI_ITEM.FrontProtection_Area_Sensor_2,true);
            SetState(DI_ITEM.FrontProtection_Area_Sensor_3,true);
            SetState(DI_ITEM.FrontProtection_Area_Sensor_4,true);
            SetState(DI_ITEM.BackProtection_Area_Sensor_1,true);
            SetState(DI_ITEM.BackProtection_Area_Sensor_2,true);
            SetState(DI_ITEM.BackProtection_Area_Sensor_3,true);
            SetState(DI_ITEM.BackProtection_Area_Sensor_4, true);

        }
        public void SetState(DI_ITEM item,bool state)
        {
            slave.DataStore.InputDiscretes[INPUT_INDEXS[item] + 1] = state;
        }
        private void Slave_ModbusSlaveRequestReceived(object? sender, ModbusSlaveRequestEventArgs e)
        {
            var inputs = slave.DataStore.InputDiscretes;
        }

        public override void Disconnect()
        {
            slave.Dispose();
            slave = null;
        }

        public override bool IsConnected()
        {
            return slave != null;
        }
    }
}
