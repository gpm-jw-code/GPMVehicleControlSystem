using Newtonsoft.Json;

namespace GPMVehicleControlSystem.Models.AGVDispatch.Messages
{
    public class clsSimpleReturnWithTimestampMessage : MessageBase
    {
        internal override string HeaderKey { get; set; }
        public new Dictionary<string, SimpleRequestResponseWithTimeStamp> Header { get; set; } = new Dictionary<string, SimpleRequestResponseWithTimeStamp>();
        internal SimpleRequestResponse ReturnData => Header[HeaderKey];
    }


    public class clsSimpleReturnMessage : MessageBase
    {
        internal override string HeaderKey { get; set; }
        public new Dictionary<string, SimpleRequestResponse> Header { get; set; } = new Dictionary<string, SimpleRequestResponse>();
        internal SimpleRequestResponse ReturnData => Header[HeaderKey];
    }

    public class SimpleRequestResponseWithTimeStamp : SimpleRequestResponse
    {
        [JsonProperty("Time Stamp")]
        public string TimeStamp { get; set; }

    }

    public class SimpleRequestResponse
    {
        [JsonProperty("Return Code")]
        public int ReturnCode { get; set; }

    }
}
