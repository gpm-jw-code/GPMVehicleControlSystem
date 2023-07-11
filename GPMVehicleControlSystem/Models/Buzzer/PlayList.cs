namespace GPMVehicleControlSystem.Models.Buzzer
{
    public class clsPlayList
    {
        public enum PLAY_ITEM
        {
            Alarm, Moving, Action
        }
        public clsPlayList()
        {
        }
        public string sounds_folder = Path.Combine(Environment.CurrentDirectory, "param/sounds");
        public string Alarm =>"alarm";
        public string Moving => "move";
        public string Action => "action";
    }
}
