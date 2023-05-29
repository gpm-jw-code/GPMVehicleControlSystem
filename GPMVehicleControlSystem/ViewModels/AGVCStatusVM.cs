using RosSharp.RosBridgeClient.MessageTypes.Geometry;
using RosSharp.RosBridgeClient.MessageTypes.Std;
using AGV_VMS.ViewModels;
using static GPMVehicleControlSystem.Models.VehicleControl.Vehicle;
using AGVSystemCommonNet6.AGVDispatch.Messages;
using AGVSystemCommonNet6.Alarm.VMS_ALARM;
using AGVSystemCommonNet6.GPMRosMessageNet.Messages;
using static AGVSystemCommonNet6.clsEnums;

namespace GPMVehicleControlSystem.ViewModels
{
    public class AGVCStatusVM
    {
        public string APPVersion { get; set; } = "1.0.0";
        public bool Simulation { get; set; }
        public AGV_TYPE Agv_Type { get; set; }
        public string MainState { get; set; } = "";
        public string SubState { get; set; } = "";
        public bool IsInitialized { get; set; }
        public bool IsSystemIniting { get; set; }
        public REMOTE_MODE OnlineMode { get; set; } = REMOTE_MODE.OFFLINE;
        public OPERATOR_MODE AutoMode { get; set; } = OPERATOR_MODE.MANUAL;
        public string CarName { get; set; } = "";
        public string AGVC_ID { get; set; } = "";
        public string CST_Data { get; set; } = "";
        public int Tag { get; set; }
        public BatteryStateVM BatteryStatus { get; set; } = new BatteryStateVM(); //TODO Multi battery data should define
        public double Mileage { get; set; }
        public Pose Pose { get; set; } = new Pose();
        public double Angle { get; set; } = -1;
        public string AGV_Direct { get; set; }
        public BarcodeReaderState BCR_State_MoveBase { get; set; } = new BarcodeReaderState();
        public DriverState ZAxisDriverState { get; set; } = new DriverState();
        public string ZAxisActionName { get; set; } = "";
        /// <summary>
        /// 地圖比對率
        /// </summary>
        public double MapComparsionRate { get; set; }
        /// <summary>
        /// SICK 定位狀態
        /// </summary>
        public int LocStatus { get; set; }
        public bool ForkCSTExist { get; set; }
        public bool ForkFrontEndSensorTrigger { get; set; }
        public clsAlarmCode[] AlarmCodes { get; set; } = new clsAlarmCode[0];

        public clsAlarmCode NewestAlarm
        {
            get
            {
                if (AlarmCodes == null)
                    return null;

                if (AlarmCodes.Length != 0)
                {
                    return AlarmCodes.Last();
                }
                else
                    return null;
            }
        }

        public DriverState[] DriversStates { get; set; } = new DriverState[0];
        public int Laser_Mode { get; set; } = 0;

        public UltrasonicSensorState UltrSensorState { get; set; } = new UltrasonicSensorState();

        public bool IsAGVPoseError { get; set; } = false;

        public NavStateVM NavInfo { get; set; } = new NavStateVM();

        public string Current_LASER_MODE { get; set; } = "";

        public LightsStatesVM LightsStates { get; set; } = new LightsStatesVM();
    }
}
