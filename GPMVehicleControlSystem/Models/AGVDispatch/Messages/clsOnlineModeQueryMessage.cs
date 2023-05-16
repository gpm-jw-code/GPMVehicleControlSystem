using Newtonsoft.Json;

namespace GPMVehicleControlSystem.Models.AGVDispatch.Messages
{

    public class clsOnlineModeQueryMessage : MessageBase
    {
        internal override string HeaderKey { get; set; } = "0101";
        public Dictionary<string, OnlineModeQuery> Header { get; set; } = new Dictionary<string, OnlineModeQuery>();
    }
    public class OnlineModeQuery
    {
        [JsonProperty("Time Stamp")]
        public string TimeStamp { get; set; } = DateTime.Now.ToAGVSTimeFormat();
    }

    public class clsOnlineModeQueryResponseMessage : MessageBase
    {
        internal override string HeaderKey { get; set; } = "0102";
        public Dictionary<string, OnlineModeQueryResponse> Header { get; set; } = new Dictionary<string, OnlineModeQueryResponse>();
        internal OnlineModeQueryResponse OnlineModeQueryResponse => this.Header[HeaderKey];
    }
    public class OnlineModeQueryResponse
    {
        [JsonProperty("Time Stamp")]
        public string TimeStamp { get; set; }
        [JsonProperty("Remote Mode")]
        public REMOTE_MODE RemoteMode { get; set; }
    }
}
