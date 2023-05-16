using GPMRosMessageNet.Actions;
using GPMRosMessageNet.Messages;
using Newtonsoft.Json;
using RosSharp.RosBridgeClient.Actionlib;
using RosSharp.RosBridgeClient.MessageTypes.Geometry;
using static GPMRosMessageNet.Actions.TaskCommandGoal;

namespace GPMVehicleControlSystem.Models.AGVDispatch.Messages
{
    public class clsTaskDownloadMessage : MessageBase
    {
        internal override string HeaderKey { get; set; } = "0301";
        public Dictionary<string, clsTaskDownloadData> Header { get; set; } = new Dictionary<string, clsTaskDownloadData>();

        internal clsTaskDownloadData TaskDownload => Header[HeaderKey];

    }


    public class clsTaskDownloadData
    {
        [JsonProperty("Task Name")]
        public string Task_Name { get; set; }

        [JsonProperty("Task Simplex")]
        public string Task_Simplex { get; set; }

        [JsonProperty("Task Sequence")]
        public int Task_Sequence { get; set; }
        public clsMapPoint[] Trajectory { get; set; } = new clsMapPoint[0];

        [JsonProperty("Homing rajectory")]
        public clsMapPoint[] Homing_Trajectory { get; set; } = new clsMapPoint[0];

        [JsonProperty("Action Type")]
        public string Action_Type { get; set; }

        public clsCST[] CST { get; set; } = new clsCST[0];
        public int Destination { get; set; }
        public int Height { get; set; }

        [JsonProperty("Escape Flag")]
        public bool Escape_Flag { get; set; }
        [JsonProperty("Station Type")]
        public STATION_TYPE Station_Type { get; set; }

        internal ACTION_TYPE EAction_Type => Enum.GetValues(typeof(ACTION_TYPE)).Cast<ACTION_TYPE>().First(action_type => action_type.ToString() == Action_Type);
        internal clsMapPoint[] ExecutingTrajecory => Trajectory.Length != 0 ? Trajectory : Homing_Trajectory;
        internal List<int> TagsOfTrajectory => ExecutingTrajecory.Select(pt => pt.Point_ID).ToList();
        internal string OriTaskDataJson;
        internal TaskCommandGoal RosTaskCommandGoal
        {
            get
            {
                try
                {
                    int finalTag = ExecutingTrajecory.Last().Point_ID;
                    GUIDE_TYPE mobility_mode = EAction_Type == ACTION_TYPE.None ? GUIDE_TYPE.SLAM : EAction_Type == ACTION_TYPE.Discharge ? GUIDE_TYPE.Color_Tap_Backward : GUIDE_TYPE.Color_Tap_Forward;
                    TaskCommandGoal goal = new TaskCommandGoal();
                    goal.taskID = Task_Name;
                    goal.finalGoalID = (ushort)finalTag;
                    goal.mobilityModes = (ushort)mobility_mode;
                    goal.planPath = new RosSharp.RosBridgeClient.MessageTypes.Nav.Path()
                    {

                    };
                    var poses = ExecutingTrajecory.Select(point => new PoseStamped()
                    {
                        header = new RosSharp.RosBridgeClient.MessageTypes.Std.Header
                        {
                            seq = (uint)point.Point_ID,
                            frame_id = "map",
                            stamp = DateTime.Now.ToStdTime(),
                        },
                        pose = new Pose()
                        {
                            position = new Point(point.X, point.Y, 0),
                            orientation = point.Theta.ToQuaternion()
                        }
                    }).ToArray();

                    var pathInfo = ExecutingTrajecory.Select(point => new PathInfo()
                    {
                        tagid = (ushort)point.Point_ID,
                        laserMode = (ushort)point.Laser,
                        direction = (ushort)point.Control_Mode.Spin,
                        map = point.Map_Name,
                        changeMap = 0,
                        speed = point.Speed,
                        ultrasonicDistance = point.UltrasonicDistance
                    }).ToArray();


                    goal.planPath.poses = poses;
                    goal.pathInfo = pathInfo;
                    return goal;
                }
                catch (Exception ec)
                {
                    LOG.ERROR("RosTaskCommandGoal_取得ROS任務Goal物件時發生錯誤", ec);
                    return new TaskCommandGoal();

                }

            }
        }
    }


    public class clsTaskDownloadAckMessage : MessageBase
    {
        internal override string HeaderKey { get; set; } = "0302";
        public Dictionary<string, SimpleRequestResponseWithTimeStamp> Header = new Dictionary<string, SimpleRequestResponseWithTimeStamp>();
    }
    public class clsTaskDownloadAckData
    {
        [JsonProperty("Return Code")]
        public int ReturnCode { get; set; }
    }

    public class clsMapPoint
    {
        public clsMapPoint() { }
        public clsMapPoint(int index)
        {
            this.index = index;
        }
        [NonSerialized]
        public int index;
        [JsonProperty("Point ID")]
        public int Point_ID { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Theta { get; set; }
        public int Laser { get; set; }
        public double Speed { get; set; }

        [JsonProperty("Map Name")]
        public string Map_Name { get; set; } = "";
        [JsonProperty("Auto Door")]
        public clsAutoDoor Auto_Door { get; set; } = new clsAutoDoor();
        [JsonProperty("Control Mode")]
        public clsControlMode Control_Mode { get; set; } = new clsControlMode();
        public double UltrasonicDistance { get; set; } = 0;
    }
    public class clsAutoDoor
    {
        [JsonProperty("Key Name")]
        public string Key_Name { get; set; }
        [JsonProperty("Key Password")]
        public string Key_Password { get; set; }
    }
    public class clsControlMode
    {
        public int Dodge { get; set; }
        public int Spin { get; set; }
    }

    public class clsCST
    {
        [JsonProperty("CST ID")]
        public string CST_ID { get; set; }
        [JsonProperty("CST Type")]
        public int CST_Type { get; set; }
    }

}
