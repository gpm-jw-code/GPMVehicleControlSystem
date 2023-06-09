using static GPMVehicleControlSystem.VehicleControl.DIOModule.clsDIModule;

namespace GPMVehicleControlSystem.VehicleControl.DIOModule
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

        internal DI_ITEM DI_item
        {
            get
            {
               return  Enum.GetValues(typeof(DI_ITEM)).Cast<DI_ITEM>().First(di => di.ToString() == Name);
            }
        }

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
                    Task.Factory.StartNew(() =>
                    {
                        if (_State)
                            OnSignalON?.Invoke(this, EventArgs.Empty);
                        else
                            OnSignalOFF?.Invoke(this, EventArgs.Empty);
                    });
                }
            }
        }


        private bool _State;
        internal ushort index;
    }
}
