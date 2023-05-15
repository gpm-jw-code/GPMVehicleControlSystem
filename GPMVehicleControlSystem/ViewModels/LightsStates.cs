namespace AGV_VMS.ViewModels
{
    public class LightsStatesVM
    {
        public bool Front { get; set; }
        public bool Back { get; set; }
        public bool Right { get; set; }
        public bool Left { get; set; }

        public bool Run { get; set; }
        public bool Down { get; set; }
        public bool Idle { get; set; }
        public bool Online { get; set; }
    }
}