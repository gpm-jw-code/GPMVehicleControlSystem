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
            Alarm = Path.Combine(sounds_folder, "alarm.wav");
            Moving = Path.Combine(sounds_folder, "move.wav");
            Action = Path.Combine(sounds_folder, "action.wav");
        }
        private string sounds_folder => Environment.CurrentDirectory;
        public string Alarm { get; set; } = "D:\\sounds\\alarm.wav";
        public string Moving { get; set; } = "D:\\sounds\\move.wav";
        public string Action { get; set; } = "D:\\sounds\\action.wav";
    }
}
