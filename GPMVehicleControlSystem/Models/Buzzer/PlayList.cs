namespace GPMVehicleControlSystem.Models.Buzzer
{
    public class clsPlayList
    {
        public enum PLAY_ITEM
        {
            Alarm,Moving,Action
        }
        public string Alarm { get; set; } = "D:\\sounds\\alarm.wav";
        public string Moving { get; set; } = "D:\\sounds\\move.wav";
        public string Action { get; set; } = "D:\\sounds\\action.wav";
    }
}
