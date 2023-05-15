using SQLite;
using System.Security.Claims;

namespace GPMVehicleControlSystem.Models.Alarm 
{
    public class clsAlarmCode
    {
        public enum LEVEL
        {
            Alarm, Warning
        }

        [PrimaryKey]
        public DateTime Time { get; set; }
        public int Code { get; set; }
        public string Description { get; set; } = "";
        public string CN { get; set; } = "";
        public LEVEL ELevel { get; set; }

        public string Level => ELevel.ToString();
        public AlarmCodes EAlarmCode
        {
            get
            {
                return Enum.GetValues(typeof(AlarmCodes)).Cast<AlarmCodes>().First(ac => (int)ac == Code);
            }
        }

        public clsAlarmCode Clone()
        {
            return new clsAlarmCode
            {
                CN = CN,
                Code = Code,
                Description = Description,

            };
        }
    }

}
