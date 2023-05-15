using System.Net.Sockets;

namespace GPMVehicleControlSystem.Models.Abstracts
{
    public interface ISocketCommunication
    {
    }


    public class clsSocketState
    {
        public const int buffer_size = 65535;
        public NetworkStream stream;
        public byte[] buffer = new byte[buffer_size];
        internal int offset = 0;
        internal string revStr = "";
        internal ManualResetEvent waitSignal = new ManualResetEvent(false);

        internal void Reset()
        {
            revStr = "";
            offset = 0;
            buffer = new byte[buffer_size];
        }
    }
}
