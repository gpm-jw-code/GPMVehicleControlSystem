namespace GPMVehicleControlSystem.ViewModels
{
    public class NavStateVM
    {
        public string Destination { get; set; } = "";
        public double Speed_max_limit { get; set; } = -1;
        
        public int[] PathPlan { get; set; } = new int[0];
    }
}
