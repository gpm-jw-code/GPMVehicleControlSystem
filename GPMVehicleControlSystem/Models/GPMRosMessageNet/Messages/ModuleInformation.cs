

using RosSharp.RosBridgeClient;

namespace GPMRosMessageNet.Messages
{
    public class ModuleInformation : Message
    {
        public const string RosMessageName = "gpm_msgs/ModuleInformation";

        public NavigationState nav_state { get; set; } = new NavigationState();
        public BarcodeReaderState reader { get; set; } = new BarcodeReaderState();
        public GpmImuMsg IMU { get; set; } = new GpmImuMsg();
        public DriversState Wheel_Driver { get; set; } = new DriversState();
        public DriverState Action_Driver { get; set; } = new DriverState();
        public BatteryState Battery { get; set; } = new BatteryState();
        public CSTReaderState CSTReader { get; set; } = new CSTReaderState();
        public GuideSensor GuideSensor { get; set; } = new GuideSensor();
        public PinsState PinsState { get; set; } = new PinsState();
        public UltrasonicSensorState UltrasonicSensor { get; set; } = new UltrasonicSensorState();
        public AlarmCodeMsg[] AlarmCode { get; set; } = new AlarmCodeMsg[0];
        public double Mileage { get; set; } = 6.9;

        public ModuleInformation()
        {
            this.nav_state = new NavigationState();
            this.reader = new BarcodeReaderState();
            this.IMU = new GpmImuMsg();
            this.Wheel_Driver = new DriversState();
            this.Action_Driver = new DriverState();
            this.Battery = new BatteryState();
            this.CSTReader = new CSTReaderState();
            this.GuideSensor = new GuideSensor();
            this.PinsState = new PinsState();
            this.UltrasonicSensor = new UltrasonicSensorState();
            this.AlarmCode = new AlarmCodeMsg[0];
            this.Mileage = 6.9;
        }

        public ModuleInformation(NavigationState nav_state, BarcodeReaderState reader, GpmImuMsg IMU, DriversState Wheel_Driver, DriverState Action_Driver, BatteryState Battery, CSTReaderState CSTReader, GuideSensor GuideSensor, PinsState PinsState, UltrasonicSensorState UltrasonicSensor, AlarmCodeMsg[] AlarmCode, double Mileage)
        {
            this.nav_state = nav_state;
            this.reader = reader;
            this.IMU = IMU;
            this.Wheel_Driver = Wheel_Driver;
            this.Action_Driver = Action_Driver;
            this.Battery = Battery;
            this.CSTReader = CSTReader;
            this.GuideSensor = GuideSensor;
            this.PinsState = PinsState;
            this.UltrasonicSensor = UltrasonicSensor;
            this.AlarmCode = AlarmCode;
            this.Mileage = Mileage;
        }
    }
}
