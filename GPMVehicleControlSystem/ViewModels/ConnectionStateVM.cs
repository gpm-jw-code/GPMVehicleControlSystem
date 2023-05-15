using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Text.Json.Serialization;

namespace GPMVehicleControlSystem.ViewModels
{
    public class ConnectionStateVM
    {
        public enum CONNECTION
        {
            CONNECTED,
            DISCONNECT,
            CONNECTING
        }

        [Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))]
        public CONNECTION RosbridgeServer { get; set; } = CONNECTION.CONNECTING;


        [Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))]
        public CONNECTION VMS { get; set; } = CONNECTION.CONNECTING;

        [Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))]
        public CONNECTION AGVC { get; set; } = CONNECTION.CONNECTING;


        [Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))]
        public CONNECTION WAGO { get; set; } = CONNECTION.CONNECTING;

    }
}
