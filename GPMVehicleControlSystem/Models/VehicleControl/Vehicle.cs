using GPMRosMessageNet.Messages;
using GPMVehicleControlSystem.Models.AGVDispatch.Messages;
using GPMVehicleControlSystem.Models.Buzzer;
using GPMVehicleControlSystem.Models.Log;
using GPMVehicleControlSystem.Models.VehicleControl.AGVControl;
using GPMVehicleControlSystem.Models.VehicleControl.DIOModule;
using GPMVehicleControlSystem.Models.VehicleControl.VehicleComponent;
using GPMVehicleControlSystem.Tools;
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
            AUTO,
            MANUAL
        }

        public enum MAIN_STATUS
        {
            IDLE = 1, RUN = 2, DOWN = 3, Charging = 4
        }
        public enum SUB_STATUS
        {
            IDLE = 1, RUN = 2, DOWN = 3, Charging = 4,
            Initialize = 5
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
        /// <summary>
        /// Online/Offline 模式
        /// </summary>
        public REMOTE_MODE Remote_Mode { get; private set; } = REMOTE_MODE.OFFLINE;
        /// <summary>
        /// 手動/自動模式
        /// </summary>
        public OPERATOR_MODE Operation_Mode { get; private set; } = OPERATOR_MODE.MANUAL;

        public MAIN_STATUS Main_Status { get; private set; } = MAIN_STATUS.DOWN;
        public bool AGV_Reset_Flag { get; private set; } = false;

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
                    BuzzerPlayer.BuzzerStop();
                    if (value == SUB_STATUS.DOWN)
                    {
                        BuzzerPlayer.BuzzerAlarm();
                    }
                    else if (value == SUB_STATUS.RUN)
                    {
                        if (CarController.IsRunning)
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
            AGVSConnection.Start();


            StatusLighter.DOWN();
            StatusLighter.OFFLINE();
            BuzzerPlayer.BuzzerAlarm();
            IsSystemInitialized = true;

        }
        private void EventsRegist()
        {
            AGVDispatch.AGVSMessageFactory.OnVCSRunningDataRequest = GenRunningStateReportData;
            AGVSConnection.OnTaskDownload += AGVSTaskDownload;
            AGVSConnection.OnRemoteModeChanged = AGVSRemoteModeChangeReq;
            AGVSConnection.OnTaskResetReq = AGVSTaskResetReqHandle;
            CarController.OnModuleInformationUpdated += CarController_OnModuleInformationUpdated;
            CarController.OnTaskActionFinishAndSuccess += AGVMoveTaskActionSuccessHandle;
            CarController.OnMoveTaskStart += CarController_OnMoveTaskStart;
            WagoDI.OnEMO += CarController.EMOHandler;
            WagoDI.OnEMO += (s, e) => Console.WriteLine("EMO Handle Process 2");
            WagoDI.OnResetButtonPressed += WagoDO.ResetMotor;
            WagoDI.OnResetButtonPressed += WagoDI_OnResetButtonPressed;
            WagoDI.OnFrontFarAreaLaserTrigger += CarController.FarAreaLaserTriggerHandler;
            WagoDI.OnBackFarAreaLaserTrigger += CarController.FarAreaLaserTriggerHandler;
            WagoDI.OnFrontFarAreaLaserRecovery += CarController.FarAreaLaserRecoveryHandler;
            WagoDI.OnBackFarAreaLaserRecovery += CarController.FarAreaLaserRecoveryHandler;
        }

        internal void SoftwareEMO()
        {
            CarController.AbortTask();
            IsInitialized = false;
            Main_Status = MAIN_STATUS.DOWN;
            Sub_Status = SUB_STATUS.DOWN;
            AGVSRemoteModeChangeReq(REMOTE_MODE.OFFLINE);
        }

        internal async Task<bool> Initialize()
        {
            IsInitialized = false;
            Main_Status = MAIN_STATUS.DOWN;
            Sub_Status = SUB_STATUS.Initialize;

            AGVSConnection.TryTaskFeedBackAsync(CarController.RunningTaskData, 0, TASK_RUN_STATUS.NO_MISSION);

            await Task.Delay(2000);
            IsInitialized = true;
            Main_Status = MAIN_STATUS.IDLE;
            Sub_Status = SUB_STATUS.IDLE;
            return true;
        }

        internal async Task<bool> CancelInitialize()
        {
            return true;
        }

        internal async Task ResetAlarmsAsync()
        {
            if (WheelDrivers.Any(dr => dr.State != VehicleComponent.Abstracts.CarComponent.STATE.NORMAL) | WagoDI.GetState(clsDIModule.DI_ITEM.Horizon_Motor_Error_1) | WagoDI.GetState(clsDIModule.DI_ITEM.Horizon_Motor_Error_2))
                await WagoDO.ResetMotor();
            BuzzerPlayer.BuzzerStop();
            return;
        }


        private void CarController_OnMoveTaskStart(object? sender, clsTaskDownloadData taskData)
        {

        }

        /// <summary>
        /// 成功完成移動任務的處理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="taskData"></param>
        private async void AGVMoveTaskActionSuccessHandle(object? sender, clsTaskDownloadData taskData)
        {
            BuzzerPlayer.BuzzerStop();
            Main_Status = MAIN_STATUS.IDLE;
            Sub_Status = SUB_STATUS.IDLE;
            StatusLighter.IDLE();
            try
            {
                int indexOfCurrentTag = taskData.ActionTrajecory.Length - 1;
                //int indexOfCurrentTag = taskData.ActionTrajecory.ToList().IndexOf(taskData.ActionTrajecory.First(pt => pt.Point_ID == Navigation.Data.lastVisitedNode.data));
                bool isActionFinish = Navigation.Data.lastVisitedNode.data == taskData.Destination;
                TASK_RUN_STATUS task_status = TASK_RUN_STATUS.NO_MISSION;
                if (isActionFinish)
                    task_status = TASK_RUN_STATUS.ACTION_FINISH;
                else
                    task_status = TASK_RUN_STATUS.NAVIGATING;

                await AGVSConnection.TryTaskFeedBackAsync(taskData, indexOfCurrentTag, task_status);
            }
            catch (Exception ex)
            {
                Log.LOG.Error("AGVMoveTaskActionSuccessHandle", ex);
            }
        }

        private bool AGVSTaskResetReqHandle(RESET_MODE mode)
        {
            Main_Status = MAIN_STATUS.IDLE;
            Sub_Status = SUB_STATUS.DOWN;
            AGV_Reset_Flag = true;
            Task.Factory.StartNew(() => CarController.AbortTask(mode));
            return true;
        }

        private bool AGVSRemoteModeChangeReq(REMOTE_MODE mode)
        {
            if (mode != Remote_Mode)
            {

                Task reqTask = new Task(async () =>
                {

                    (bool success, RETURN_CODE return_code) result = await Online_Mode_Switch(mode);
                    if (result.success)
                    {
                        Remote_Mode = mode;
                        Console.WriteLine($"[Online Mode Change] 請求 {mode} 成功!");
                        if (mode == REMOTE_MODE.ONLINE)
                            StatusLighter.ONLINE();
                        else
                            StatusLighter.OFFLINE();
                    }
                    else
                    {
                        Console.WriteLine($"[Online Mode Change] 請求 {mode} 失敗!(Return Code = {(int)result.return_code}-{result.return_code}) 現在是 {Remote_Mode}");
                    }
                });
                reqTask.Start();
            }
            return true;
        }

        private bool AGVSTaskDownload(clsTaskDownloadData taskDownloadData)
        {
            Main_Status = MAIN_STATUS.RUN;
            Sub_Status = SUB_STATUS.RUN;
            StatusLighter.RUN();
            AGV_Reset_Flag = false;
            CarController.AGVSTaskDownloadHandler(taskDownloadData);

            if (taskDownloadData.EAction_Type == ACTION_TYPE.None)
                BuzzerPlayer.BuzzerMoving();
            else
                BuzzerPlayer.BuzzerAction();
            //Task.Delay(1000).ContinueWith((task) =>
            //{
            //    AGVSConnection.TryTaskFeedBackAsync(taskDownloadData, 0, TASK_RUN_STATUS.ACTION_START);
            //});
            return true;
        }

        private RunningStatus GenRunningStateReportData()
        {
            return new RunningStatus
            {
                Cargo_Status = 0,
                AGV_Status = Main_Status,
                Electric_Volume = new double[2] { Battery.Data.batteryLevel, Battery.Data.batteryLevel },
                Last_Visited_Node = Navigation.Data.lastVisitedNode.data,
                Corrdination = new RunningStatus.clsCorrdination()
                {
                    X = Navigation.Data.robotPose.pose.position.x,
                    Y = Navigation.Data.robotPose.pose.position.y,
                    Theta = CalculateTheta(Navigation.Data.robotPose.pose.orientation)
                },
                CSTID = new string[] { CSTReader.Data.data },
                Odometry = Odometry,
                AGV_Reset_Flag = AGV_Reset_Flag
            };
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

        internal async Task<(bool success, RETURN_CODE return_code)> Online_Mode_Switch(REMOTE_MODE mode)
        {
            (bool success, RETURN_CODE return_code) result = await AGVSConnection.TrySendOnlineModeChangeRequest(BarcodeReader.CurrentTag, mode);
            if (!result.success)
                Log.LOG.Error($"車輛上線失敗 : Return Code : {result.return_code}");
            else
                Remote_Mode = mode;
            return result;
        }
    }
}
