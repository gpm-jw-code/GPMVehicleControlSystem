namespace GPMVehicleControlSystem.Models.VehicleControl.DIOModule
{
    public class clsIOSignal
    {
        public clsIOSignal(string Name, string Address)
        {
            this.Name = Name;
            this.Address = Address;
        }
        public event EventHandler OnSignalON;
        public event EventHandler OnSignalOFF;



        public string Name { get; }
        public string Address { get; }
        public bool State
        {
            get => _State;
            set
            {
                if (_State != value)
                {
                    //Console.WriteLine($"{this.ToJson()}");
                    _State = value;
                    if (_State)
                        OnSignalON?.Invoke(this, EventArgs.Empty);
                    else
                        OnSignalOFF?.Invoke(this, EventArgs.Empty);
                }
            }
        }


        private bool _State;
    }
}
