using Newtonsoft.Json;
using RosSharp.RosBridgeClient.MessageTypes.Geometry;
using RosSharp.RosBridgeClient.MessageTypes.Std;
using static GPMVehicleControlSystem.Models.AGVDispatch.clsAGVSConnection;

namespace GPMVehicleControlSystem
{
    public static class Extension
    {
        public static Time ToStdTime(this DateTime _time)
        {
            return new Time()
            {
                secs = (uint)(_time.Subtract(new DateTime(1970, 1, 1)).TotalSeconds),
                nsecs = (uint)(_time.Millisecond * 1000000),
            };
        }

        public static string ToAGVSTimeFormat(this DateTime _time)
        {
            return _time.ToString("yyyyMMdd HH:mm:ss");
        }
        public static Quaternion ToQuaternion(this double Theta)
        {
            double yaw_radians = (float)Theta * Math.PI / 180.0;
            double cos_yaw = Math.Cos(yaw_radians / 2.0);
            double sin_yaw = Math.Sin(yaw_radians / 2.0);
            return new Quaternion(0.0f, 0.0f, (float)sin_yaw, (float)cos_yaw);
        }
        public static string ToJson(this object obj)
        {
            try
            {
                return JsonConvert.SerializeObject(obj, Formatting.Indented);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return "{}";
            }
        }
    }
}
