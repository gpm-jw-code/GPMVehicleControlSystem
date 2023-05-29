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
        public string Alarm => $"{sounds_folder}/alarm.wav";
        public string Moving => $"{sounds_folder}/move.wav";
        public string Action => $"{sounds_folder}/action.wav";
    }
}
