using Newtonsoft.Json;

namespace GPMVehicleControlSystem.Models.AGVDispatch.Messages
{
    public class clsOnlineModeRequestMessage : MessageBase
    {
        internal override string HeaderKey { get; set; } = "0103";
        public Dictionary<string, OnlineModeRequest> Header { get; set; } = new Dictionary<string, OnlineModeRequest>();

    }


    public class OnlineModeRequest
    {
        [JsonProperty("Time Stamp")]
        public string TimeStamp { get; set; }
        [JsonProperty("Mode Request")]
        public REMOTE_MODE ModeRequest { get; set; }

        [JsonProperty("Current Node")]
        public int CurrentNode { get; set; }
    }

    public class clsOnlineModeRequestResponseMessage : MessageBase
    {
        internal override string HeaderKey { get; set; } = "0104";
        public Dictionary<string, SimpleRequestResponseWithTimeStamp> Header { get; set; } = new Dictionary<string, SimpleRequestResponseWithTimeStamp>();
        public RETURN_CODE ReturnCode => (RETURN_CODE)Header[HeaderKey].ReturnCode;
    }


    
}
