using Newtonsoft.Json;
using static GPMVehicleControlSystem.Models.VehicleControl.Vehicle;

namespace GPMVehicleControlSystem.Models.AGVDispatch.Messages
{
    public class clsRunningStatusReportMessage : MessageBase
    {
        internal override string HeaderKey { get; set; } = "0105";
        public Dictionary<string, RunningStatus> Header { get; set; } = new Dictionary<string, RunningStatus>();
    }

    public class RunningStatus
    {
        [JsonProperty("Time Stamp")]
        public string Time_Stamp { get; set; } = DateTime.Now.ToAGVSTimeFormat();

        public clsCorrdination Corrdination { get; set; } = new clsCorrdination();
        [JsonProperty("Last Visited Node")]
        public int Last_Visited_Node { get; set; } = 0;
        /// <summary>
        /// 1. IDLE: active but no mission
        /// 2. RUN: executing mission
        /// 3. DOWN: alarm or error
        /// 4. Charging: in charging,
        /// 
        /// </summary>
        [JsonProperty("AGV Status")]
        public MAIN_STATUS AGV_Status { get; set; } = MAIN_STATUS.DOWN;
        [JsonProperty("Escape Flag")]
        public bool Escape_Flag { get; set; } = false;
        [JsonProperty("Sensor Status")]

        public Dictionary<string, int> Sensor_Status { get; set; } = new Dictionary<string, int>
        {
            {"Barcode Reader" ,0 },
            {"Guide Sensor" ,0 },
            {"LiDAR Sensor" ,0 },
            {"Driver" ,0 },
            {"Tag_Reader" ,0 },
            {"G-Sensor" ,0 },
        };

        [JsonProperty("CPU Usage Percent")]
        public double CPU_Usage_Percent { get; set; } = 0;

        [JsonProperty("RAM Usage Percent")]
        public double RAM_Usage_Percent { get; set; } = 0;

        [JsonProperty("AGV Reset Flag")]
        public bool AGV_Reset_Flag { get; set; } = true;
        [JsonProperty("Signal Strength")]
        public double Signal_Strength { get; set; } = 0;
        
        [JsonProperty("Cargo Status")]
        public int Cargo_Status { get; set; } = 0;

        public string[] CSTID { get; set; } = new string[0];
        public double Odometry { get; set; } = 0;
       

        [JsonProperty("Electric Volume")]
        public double[] Electric_Volume { get; set; } = new double[2] { 0, 0 };
        
        [JsonProperty("Alarm Code")]
        public clsAlarmCode[] Alarm_Code { get; set; } = new clsAlarmCode[0];
        [JsonProperty("Fork Height")]
        public double Fork_Height { get; set; } 

        public class clsCorrdination
        {
            public double X { get; set; }
            public double Y { get; set; } 
            public double Theta { get; set; }
        }
        public class clsAlarmCode
        {
            [JsonProperty("Alarm ID")]
            public int Alarm_ID { get; set; }
            [JsonProperty("Alarm Level")]
            public int Alarm_Level { get; set; }
            [JsonProperty("Alarm Category")]
            public int Alarm_Category { get; set; }
            [JsonProperty("Alarm Description")]
            public string Alarm_Description { get; set; } = "";
        }
    }
    public class clsRunningStatusReportResponseMessage : MessageBase
    {
        internal override string HeaderKey { get; set; } = "0106";
        public Dictionary<string, SimpleRequestResponseWithTimeStamp> Header { get; set; }
        internal SimpleRequestResponseWithTimeStamp RuningStateReportAck => Header[HeaderKey];

    }

}
