using GPMRosMessageNet.Messages;
using GPMVehicleControlSystem.Models.AGVDispatch.Messages;
using GPMVehicleControlSystem.Models.Buzzer;
using GPMVehicleControlSystem.Models.VehicleControl.AGVControl;
using GPMVehicleControlSystem.Models.VehicleControl.DIOModule;
using GPMVehicleControlSystem.Models.VehicleControl.VehicleComponent;
using GPMVehicleControlSystem.Tools;
using System.Threading.Tasks;
using static GPMVehicleControlSystem.Models.VehicleControl.VehicleComponent.clsLaser;

namespace GPMVehicleControlSystem.Models.VehicleControl
{
    public class Vehicle
    {
        public enum AGV_TYPE
        {
            FORK, SUBMERGED_SHIELD
        }
        public enum OPERATOR_MODE
        {
            MANUAL,
            AUTO,
        }

        public enum MAIN_STATUS
        {
            IDLE = 1, RUN = 2, DOWN = 3, Charging = 4
        }
        public enum SUB_STATUS
        {
            IDLE = 1, RUN = 2, DOWN = 3, Charging = 4,
            Initializing = 5,
            ALARM = 6,
            WARNING = 7
        }
        public clsDirectionLighter DirectionLighter { get; set; }
        public clsStatusLighter StatusLighter { get; set; }

        public AGVDispatch.clsAGVSConnection AGVSConnection;

        public clsDOModule WagoDO;
        public clsDIModule WagoDI;
        public CarController CarController;

        public clsLaser Laser;
        public string CarName { get; set; }
        public string SID { get; set; }

        public AGVPILOT Pilot { get; set; }
        public clsNavigation Navigation = new clsNavigation();
        public clsBattery Battery = new clsBattery();
        public clsIMU IMU = new clsIMU();
        public clsGuideSensor GuideSensor = new clsGuideSensor();
        public clsBarcodeReader BarcodeReader = new clsBarcodeReader();
        public clsDriver[] WheelDrivers = new clsDriver[] {
             new clsDriver{ location = clsDriver.DRIVER_LOCATION.LEFT},
             new clsDriver{ location = clsDriver.DRIVER_LOCATION.RIGHT},
        };

        public clsCSTReader CSTReader = new clsCSTReader();
        /// <summary>
        /// 里程數
        /// </summary>
        public double Odometry;



        private REMOTE_MODE _Remote_Mode = REMOTE_MODE.OFFLINE;
        /// <summary>
        /// Online/Offline 模式
        /// </summary>
        public REMOTE_MODE Remote_Mode
        {
            get => _Remote_Mode;
            set
            {
                _Remote_Mode = value;
                if (value == REMOTE_MODE.ONLINE)
                {
                    StatusLighter.ONLINE();
                }
                else
                    StatusLighter.OFFLINE();
            }
        }
        /// <summary>
        /// 手動/自動模式
        /// </summary>
        public OPERATOR_MODE Operation_Mode { get; internal set; } = OPERATOR_MODE.MANUAL;

        public MAIN_STATUS Main_Status { get; internal set; } = MAIN_STATUS.DOWN;
        public bool AGV_Reset_Flag { get; internal set; } = false;

        public MoveControl ManualController => CarController.ManualController;

        public AGV_TYPE AgvType { get; internal set; } = AGV_TYPE.SUBMERGED_SHIELD;
        public bool SimulationMode { get; internal set; } = false;
        public bool IsInitialized { get; internal set; }
        public bool IsSystemInitialized { get; internal set; }
        private SUB_STATUS _Sub_Status = SUB_STATUS.DOWN;
        public SUB_STATUS Sub_Status
        {
            get => _Sub_Status;
            set
            {
                if (_Sub_Status != value)
                {
                    if (value != SUB_STATUS.WARNING)
                        BuzzerPlayer.BuzzerStop();

                    if (value == SUB_STATUS.DOWN | value == SUB_STATUS.ALARM | value == SUB_STATUS.Initializing)
                    {
                        if (value == SUB_STATUS.DOWN | value == SUB_STATUS.Initializing)
                            Main_Status = MAIN_STATUS.DOWN;
                        StatusLighter.DOWN();
                        BuzzerPlayer.BuzzerAlarm();
                    }
                    else if (value == SUB_STATUS.IDLE)
                    {
                        Main_Status = MAIN_STATUS.IDLE;
                        StatusLighter.IDLE();
                        DirectionLighter.CloseAll();
                    }
                    else if (value == SUB_STATUS.Charging)
                    {
                        Main_Status = MAIN_STATUS.Charging;
                    }
                    else if (value == SUB_STATUS.RUN)
                    {
                        Main_Status = MAIN_STATUS.RUN;
                        StatusLighter.RUN();
                        if (CarController.IsAGVExecutingTask)
                        {
                            if (CarController.RunningTaskData.EAction_Type == ACTION_TYPE.None)
                                BuzzerPlayer.BuzzerMoving();
                            else
                                BuzzerPlayer.BuzzerAction();
                        }
                    }

                    _Sub_Status = value;
                }
            }
        }

        public Vehicle()
        {

            IsSystemInitialized = false;
            string AGVS_IP = AppSettingsHelper.GetValue<string>("VCS:Connections:AGVS:IP");
            int AGVS_Port = AppSettingsHelper.GetValue<int>("VCS:Connections:AGVS:Port");
            string AGVS_LocalIP = AppSettingsHelper.GetValue<string>("VCS:Connections:AGVS:LocalIP");

            string Wago_IP = AppSettingsHelper.GetValue<string>("VCS:Connections:Wago:IP");
            int Wago_Port = AppSettingsHelper.GetValue<int>("VCS:Connections:Wago:Port");

            string RosBridge_IP = AppSettingsHelper.GetValue<string>("VCS:Connections:RosBridge:IP");
            int RosBridge_Port = AppSettingsHelper.GetValue<int>("VCS:Connections:RosBridge:Port");

            SID = AppSettingsHelper.GetValue<string>("VCS:SID");
            CarName = AppSettingsHelper.GetValue<string>("VCS:EQName");

            WagoDO = new clsDOModule(Wago_IP, Wago_Port);
            WagoDI = new clsDIModule(Wago_IP, Wago_Port);
            CarController = new CarController(RosBridge_IP, RosBridge_Port);
            AGVSConnection = new AGVDispatch.clsAGVSConnection(AGVS_IP, AGVS_Port, AGVS_LocalIP);

            DirectionLighter = new clsDirectionLighter(WagoDO);
            StatusLighter = new clsStatusLighter(WagoDO);
            Laser = new clsLaser(WagoDO, WagoDI);

            Task RosConnTask = new Task(() => CarController.Connect());
            Task WagoDOConnTask = new Task(() =>
            {
                WagoDO.Connect();
                WagoDO.StartAsync();
            });

            Task WagoDIConnTask = new Task(() =>
            {
                WagoDI.Connect();
                WagoDI.StartAsync();
            });

            RosConnTask.Start();
            WagoDOConnTask.Start();
            WagoDIConnTask.Start();
            EventsRegist();
            Laser.Mode = LASER_MODE.Bypass;
            AGVDispatch.AGVSMessageFactory.Setup(SID, CarName);

            Pilot = new AGVPILOT(this);

            BuzzerPlayer.BuzzerAlarm();
            IsSystemInitialized = true;



            Task.Factory.StartNew(async () =>
            {
                await Task.Delay(1000);
                AGVSConnection.Start();
            });

        }
        private void EventsRegist()
        {
            AGVDispatch.AGVSMessageFactory.OnVCSRunningDataRequest += GenRunningStateReportData;
            AGVSConnection.OnRemoteModeChanged = AGVSRemoteModeChangeReq;
            CarController.OnModuleInformationUpdated += CarController_OnModuleInformationUpdated;

            WagoDI.OnEMO += WagoDI_OnEMO;
            WagoDI.OnEMO += CarController.EMOHandler;
            WagoDI.OnResetButtonPressed += WagoDO.ResetMotor;
            WagoDI.OnResetButtonPressed += WagoDI_OnResetButtonPressed;
            WagoDI.OnFrontFarAreaLaserTrigger += CarController.FarAreaLaserTriggerHandler;
            WagoDI.OnBackFarAreaLaserTrigger += CarController.FarAreaLaserTriggerHandler;
            WagoDI.OnFrontFarAreaLaserRecovery += CarController.FarAreaLaserRecoveryHandler;
            WagoDI.OnBackFarAreaLaserRecovery += CarController.FarAreaLaserRecoveryHandler;

            WagoDI.OnFrontFarAreaLaserTrigger += WagoDI_OnFarAreaLaserTrigger;
            WagoDI.OnBackFarAreaLaserTrigger += WagoDI_OnFarAreaLaserTrigger;

            WagoDI.OnFrontFarAreaLaserRecovery += WagoDI_OnFarAreaLaserRecovery;
            WagoDI.OnBackFarAreaLaserRecovery += WagoDI_OnFarAreaLaserRecovery;

            WagoDI.OnFrontNearAreaLaserTrigger += WagoDI_OnNearAreaLaserTrigger;
            WagoDI.OnBackNearAreaLaserTrigger += WagoDI_OnNearAreaLaserTrigger;

            WagoDI.OnFrontNearAreaLaserRecovery += WagoDI_OnNearAreaLaserRecovery;
            WagoDI.OnBackNearAreaLaserRecovery += WagoDI_OnNearAreaLaserRecovery;


            Navigation.OnDirectionChanged += Navigation_OnDirectionChanged;
        }



        private void WagoDI_OnFarAreaLaserRecovery(object? sender, EventArgs e)
        {

            if (Operation_Mode != OPERATOR_MODE.AUTO)
                return;
            if (Main_Status == MAIN_STATUS.RUN)
                Sub_Status = SUB_STATUS.RUN;
            else if (Main_Status == MAIN_STATUS.IDLE)
                Sub_Status = SUB_STATUS.IDLE;
            else if (Main_Status == MAIN_STATUS.DOWN)
                Sub_Status = SUB_STATUS.DOWN;

        }

        private void WagoDI_OnFarAreaLaserTrigger(object? sender, EventArgs e)
        {
            if (Operation_Mode == OPERATOR_MODE.AUTO)
                Sub_Status = SUB_STATUS.WARNING;
        }

        private void WagoDI_OnNearAreaLaserRecovery(object? sender, EventArgs e)
        {
            if (Operation_Mode != OPERATOR_MODE.AUTO)
                return;
            if (Main_Status == MAIN_STATUS.RUN)
                Sub_Status = SUB_STATUS.RUN;
            else if (Main_Status == MAIN_STATUS.IDLE)
                Sub_Status = SUB_STATUS.IDLE;
            else if (Main_Status == MAIN_STATUS.DOWN)
                Sub_Status = SUB_STATUS.DOWN;
        }

        private void WagoDI_OnNearAreaLaserTrigger(object? sender, EventArgs e)
        {
            if (Operation_Mode == OPERATOR_MODE.AUTO)
                Sub_Status = SUB_STATUS.ALARM;
        }

        private void Navigation_OnDirectionChanged(object? sender, clsNavigation.AGV_DIRECTION e)
        {
            if (CarController.IsAGVExecutingTask)
            {
                DirectionLighter.LightSwitchByAGVDirection(sender, e);
                Laser.LaserChangeByAGVDirection(sender, e);
            }
        }

        private void WagoDI_OnEMO(object? sender, EventArgs e)
        {
            SoftwareEMO();
        }


        internal async Task<bool> Initialize()
        {
            IsInitialized = false;
            Sub_Status = SUB_STATUS.Initializing;
            Laser.LeftLaserBypass = true;
            Laser.RightLaserBypass = true;
            Laser.FrontLaserBypass = true;
            Laser.BackLaserBypass = true;
            Laser.Mode = LASER_MODE.Bypass;
            //AGVSConnection.TryTaskFeedBackAsync(CarController.RunningTaskData, 0, TASK_RUN_STATUS.NO_MISSION);
            StatusLighter.CloseAll();
            StatusLighter.Flash(clsDOModule.DO_ITEM.AGV_DiractionLight_Y, 200);
            StatusLighter.Flash(clsDOModule.DO_ITEM.AGV_DiractionLight_R, 200);
            StatusLighter.Flash(clsDOModule.DO_ITEM.AGV_DiractionLight_G, 200);
            await Task.Delay(2000);
            IsInitialized = true;
            StatusLighter.AbortFlash();
            Sub_Status = SUB_STATUS.IDLE;
            return true;
        }

        internal async Task<bool> CancelInitialize()
        {
            return true;
        }

        internal void SoftwareEMO()
        {
            IsInitialized = false;
            Sub_Status = SUB_STATUS.DOWN;
            CarController.EMOHandler("SoftwareEMO", EventArgs.Empty);
            AGVSRemoteModeChangeReq(REMOTE_MODE.OFFLINE);
        }

        internal async Task ResetAlarmsAsync()
        {
            if (WheelDrivers.Any(dr => dr.State != VehicleComponent.Abstracts.CarComponent.STATE.NORMAL) | WagoDI.GetState(clsDIModule.DI_ITEM.Horizon_Motor_Error_1) | WagoDI.GetState(clsDIModule.DI_ITEM.Horizon_Motor_Error_2))
                await WagoDO.ResetMotor();
            BuzzerPlayer.BuzzerStop();
            return;
        }



        /// <summary>
        /// 成功完成移動任務的處理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="taskData"></param>


        private bool AGVSRemoteModeChangeReq(REMOTE_MODE mode)
        {
            if (mode != Remote_Mode)
            {

                Task reqTask = new Task(async () =>
                {
                    if (OnlineModeChangingFlag)
                    {
                        return;
                    }
                    OnlineModeChangingFlag = true;
                    (bool success, RETURN_CODE return_code) result = await Online_Mode_Switch(mode);
                    if (result.success)
                    {
                        Remote_Mode = mode;
                        Console.WriteLine($"[Online Mode Change] 請求 {mode} 成功!");
                    }
                    else
                    {
                        Console.WriteLine($"[Online Mode Change] 請求 {mode} 失敗!(Return Code = {(int)result.return_code}-{result.return_code}) 現在是 {Remote_Mode}");
                    }
                    OnlineModeChangingFlag = false;
                });
                reqTask.Start();
            }
            return true;
        }


        private RunningStatus GenRunningStateReportData()
        {
            try
            {
                return new RunningStatus
                {

                    Cargo_Status = (!WagoDI.GetState(clsDIModule.DI_ITEM.Cst_Sensor_1) | !WagoDI.GetState(clsDIModule.DI_ITEM.Cst_Sensor_1)) ? 1 : 0,
                    AGV_Status = Main_Status,
                    Electric_Volume = new double[2] { Battery.Data.batteryLevel, Battery.Data.batteryLevel },
                    Last_Visited_Node = Navigation.Data.lastVisitedNode.data,
                    Corrdination = new RunningStatus.clsCorrdination()
                    {
                        X = Navigation.Data.robotPose.pose.position.x,
                        Y = Navigation.Data.robotPose.pose.position.y,
                        Theta = BarcodeReader.Data.theta
                    },
                    CSTID = new string[] { CSTReader.Data.data },
                    Odometry = Odometry,
                    AGV_Reset_Flag = AGV_Reset_Flag
                };
            }
            catch (Exception ex)
            {
                LOG.ERROR("GenRunningStateReportData ", ex);
                return new RunningStatus();
            }
        }

        double CalculateTheta(RosSharp.RosBridgeClient.MessageTypes.Geometry.Quaternion orientation)
        {
            double yaw;
            double x = orientation.x;
            double y = orientation.y;
            double z = orientation.z;
            double w = orientation.w;
            // 計算角度
            double siny_cosp = 2.0 * (w * z + x * y);
            double cosy_cosp = 1.0 - 2.0 * (y * y + z * z);
            yaw = Math.Atan2(siny_cosp, cosy_cosp);
            return yaw * 180.0 / Math.PI;
        }

        private void CarController_OnModuleInformationUpdated(object? sender, ModuleInformation _ModuleInformation)
        {
            Odometry = _ModuleInformation.Mileage;
            Navigation.StateData = _ModuleInformation.nav_state;
            Battery.StateData = _ModuleInformation.Battery;
            IMU.StateData = _ModuleInformation.IMU;
            GuideSensor.StateData = _ModuleInformation.GuideSensor;
            BarcodeReader.StateData = _ModuleInformation.reader;
            CSTReader.StateData = _ModuleInformation.CSTReader;
            for (int i = 0; i < _ModuleInformation.Wheel_Driver.driversState.Length; i++)
                WheelDrivers[i].StateData = _ModuleInformation.Wheel_Driver.driversState[i];
        }

        private void WagoDI_OnResetButtonPressed(object? sender, EventArgs e)
        {
            Console.WriteLine("Try Reset Alarms");
        }

        internal async Task<bool> Auto_Mode_Siwtch(OPERATOR_MODE mode)
        {
            Operation_Mode = mode;
            return true;
        }
        private bool OnlineModeChangingFlag = false;
        internal async Task<(bool success, RETURN_CODE return_code)> Online_Mode_Switch(REMOTE_MODE mode)
        {
            (bool success, RETURN_CODE return_code) result = await AGVSConnection.TrySendOnlineModeChangeRequest(BarcodeReader.CurrentTag, mode);
            if (!result.success)
                LOG.ERROR($"車輛上線失敗 : Return Code : {result.return_code}");
            else
                Remote_Mode = mode;
            return result;
        }
    }
}
