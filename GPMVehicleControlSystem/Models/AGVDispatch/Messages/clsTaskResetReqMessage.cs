using Newtonsoft.Json;

namespace GPMVehicleControlSystem.Models.AGVDispatch.Messages
{
    public class clsTaskResetReqMessage : MessageBase
    {
        internal override string HeaderKey { get; set; } = "0305";

        public Dictionary<string, TaskResetDto> Header { get; set; } = new Dictionary<string, TaskResetDto>();
        internal TaskResetDto ResetData => Header[HeaderKey];
    }


    public class TaskResetDto
    {
        [JsonProperty("Time Stamp")]
        public string Time_Stamp { get; set; }

        [JsonProperty("ResetMode Mode")]
        public RESET_MODE ResetMode { get; set; }
    }
}
