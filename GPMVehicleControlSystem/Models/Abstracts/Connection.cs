namespace GPMVehicleControlSystem.Models.Abstracts
{
    public abstract class Connection
    {
        public string IP;
        public int Port;
        public Connection()
        {

        }
        public Connection(string IP, int Port)
        {
            this.IP = IP;
            this.Port = Port;
        }
        public abstract bool Connect();
        public abstract void Disconnect();
        public abstract bool IsConnected();

    }
}
