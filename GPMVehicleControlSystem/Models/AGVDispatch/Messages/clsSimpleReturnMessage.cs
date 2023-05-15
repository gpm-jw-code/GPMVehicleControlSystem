using Newtonsoft.Json;

namespace GPMVehicleControlSystem.Models.AGVDispatch.Messages
{
    public class clsSimpleReturnMessage : MessageBase
    {
        internal override string HeaderKey { get; set; }
        public new Dictionary<string, SimpleRequestResponse> Header { get; set; } = new Dictionary<string, SimpleRequestResponse>();
        internal SimpleRequestResponse ReturnData => Header[HeaderKey];
    }
    public class SimpleRequestResponse
    {
        [JsonProperty("Time Stamp")]
        public string TimeStamp { get; set; }
        [JsonProperty("Return Code")]
        public RETURN_CODE ReturnCode { get; set; }

    }
}
