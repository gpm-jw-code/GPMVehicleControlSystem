using AGVSystemCommonNet6.Abstracts;
using AGVSystemCommonNet6.AGVDispatch;
using AGVSystemCommonNet6.AGVDispatch.Messages;
using AGVSystemCommonNet6.Alarm.VMS_ALARM;
using AGVSystemCommonNet6.GPMRosMessageNet.Messages;
using AGVSystemCommonNet6.GPMRosMessageNet.Messages.SickMsg;
using AGVSystemCommonNet6.Log;
using GPMVehicleControlSystem.Models.Buzzer;
using GPMVehicleControlSystem.Models.VehicleControl.AGVControl;
using GPMVehicleControlSystem.Models.VehicleControl.VehicleComponent;
using GPMVehicleControlSystem.Tools;
using GPMVehicleControlSystem.VehicleControl.DIOModule;
using static AGVSystemCommonNet6.Abstracts.CarComponent;
using static AGVSystemCommonNet6.clsEnums;
using static GPMVehicleControlSystem.Models.VehicleControl.VehicleComponent.clsLaser;

namespace GPMVehicleControlSystem.Models.VehicleControl
{
    /// <summary>
    /// 車子
    /// </summary>
    public partial class Vehicle
    {

        public clsDirectionLighter DirectionLighter { get; set; }
        public clsStatusLighter StatusLighter { get; set; }

        public AGVSystemCommonNet6.AGVDispatch.clsAGVSConnection AGVS;

        public clsDOModule WagoDO;
        public clsDIModule WagoDI;
        public CarController AGVC;

        public clsLaser Laser;
        public string CarName { get; set; }
        public string SID { get; set; }

        //public AGVPILOT Pilot { get; set; }
        public clsNavigation Navigation = new clsNavigation();

        public Dictionary<ushort, clsBattery> Batteries = new Dictionary<ushort, clsBattery>();

        public clsIMU IMU = new clsIMU();
        public clsGuideSensor GuideSensor = new clsGuideSensor();
        public clsBarcodeReader BarcodeReader = new clsBarcodeReader();
        public clsDriver[] WheelDrivers = new clsDriver[] {
             new clsDriver{ location = clsDriver.DRIVER_LOCATION.LEFT},
             new clsDriver{ location = clsDriver.DRIVER_LOCATION.RIGHT},
        };
        public clsSick SickData = new clsSick();
        public clsCSTReader CSTReader = new clsCSTReader();
        /// <summary>
        /// 里程數
        /// </summary>
        public double Odometry;

        private List<CarComponent> CarComponents
        {
            get
            {
                var ls = new List<CarComponent>()
                {
                    Navigation,IMU,GuideSensor, BarcodeReader,CSTReader,
                };
                ls.AddRange(Batteries.Values.ToArray());
                ls.AddRange(WheelDrivers);
                return ls;
            }
        }


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

        public MoveControl ManualController => AGVC.ManualController;

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
                        if (value != SUB_STATUS.Initializing)
                            BuzzerPlayer.BuzzerAlarm();
                        StatusLighter.DOWN();
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
                        Task.Factory.StartNew(async () =>
                        {
                            await Task.Delay(200);
                            if (AGVC.IsAGVExecutingTask)
                            {
                                if (AGVC.RunningTaskData.Action_Type == ACTION_TYPE.None)
                                    BuzzerPlayer.BuzzerMoving();
                                else
                                    BuzzerPlayer.BuzzerAction();
                            }
                        });

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
            AGVC = new CarController(RosBridge_IP, RosBridge_Port);
            AGVS = new clsAGVSConnection(AGVS_IP, AGVS_Port, AGVS_LocalIP);

            DirectionLighter = new clsDirectionLighter(WagoDO);
            StatusLighter = new clsStatusLighter(WagoDO);
            Laser = new clsLaser(WagoDO, WagoDI);

            Task RosConnTask = new Task(async () =>
            {
                await Task.Delay(1).ContinueWith(t =>
                AGVC.Connect());
                BuzzerPlayer.rossocket = AGVC.rosSocket;
                BuzzerPlayer.BuzzerAlarm();
            });

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
            AGVSMessageFactory.Setup(SID, CarName);

            //Pilot = new AGVPILOT(this);

            IsSystemInitialized = true;

            Task.Factory.StartNew(async () =>
            {
                await Task.Delay(1000);
                AGVS.Start();
            });

        }
        private void EventsRegist() //TODO EventRegist
        {
            AGVSMessageFactory.OnVCSRunningDataRequest += GenRunningStateReportData;
            AGVS.OnRemoteModeChanged = AGVSRemoteModeChangeReq;
            AGVC.OnModuleInformationUpdated += CarController_OnModuleInformationUpdated;
            AGVC.OnSickDataUpdated += CarController_OnSickDataUpdated;
            WagoDI.OnEMO += WagoDI_OnEMO;
            WagoDI.OnEMO += AGVC.EMOHandler;
            WagoDI.OnResetButtonPressing += () => ResetAlarmsAsync();
            WagoDI.OnResetButtonPressed += WagoDO.ResetMotor;
            WagoDI.OnResetButtonPressed += WagoDI_OnResetButtonPressed;
            WagoDI.OnFrontArea1LaserTrigger += AGVC.FarArea1LaserTriggerHandler;
            WagoDI.OnBackArea1LaserTrigger += AGVC.FarArea1LaserTriggerHandler;
            WagoDI.OnFrontArea2LaserTrigger += AGVC.FarArea2LaserTriggerHandler;
            WagoDI.OnBackArea2LaserTrigger += AGVC.FarArea2LaserTriggerHandler;

            WagoDI.OnFrontArea1LaserRecovery += AGVC.FrontFarArea1LaserRecoveryHandler;
            WagoDI.OnFrontArea2LaserRecovery += AGVC.FrontFarArea2LaserRecoveryHandler;
            WagoDI.OnBackArea1LaserRecovery += AGVC.BackFarArea1LaserRecoveryHandler;
            WagoDI.OnBackArea2LaserRecovery += AGVC.BackFarArea2LaserRecoveryHandler;

            WagoDI.OnFrontArea1LaserTrigger += WagoDI_OnFarAreaLaserTrigger;
            WagoDI.OnBackArea1LaserTrigger += WagoDI_OnFarAreaLaserTrigger;
            WagoDI.OnFrontArea2LaserTrigger += WagoDI_OnFarAreaLaserTrigger;
            WagoDI.OnFrontArea2LaserTrigger += WagoDI_OnNearAreaLaserTrigger;
            WagoDI.OnBackArea2LaserTrigger += WagoDI_OnNearAreaLaserTrigger;

            WagoDI.OnBackArea2LaserTrigger += WagoDI_OnFarAreaLaserTrigger;



            WagoDI.OnFrontArea1LaserRecovery += WagoDI_OnFarAreaLaserRecovery;
            WagoDI.OnBackArea1LaserRecovery += WagoDI_OnFarAreaLaserRecovery;
            WagoDI.OnFrontArea2LaserRecovery += WagoDI_OnFarAreaLaserRecovery;
            WagoDI.OnBackArea2LaserRecovery += WagoDI_OnFarAreaLaserRecovery;



            WagoDI.OnFrontNearAreaLaserTrigger += WagoDI_OnNearAreaLaserTrigger;
            WagoDI.OnBackNearAreaLaserTrigger += WagoDI_OnNearAreaLaserTrigger;

            WagoDI.OnFrontNearAreaLaserRecovery += WagoDI_OnNearAreaLaserRecovery;
            WagoDI.OnBackNearAreaLaserRecovery += WagoDI_OnNearAreaLaserRecovery;


            Navigation.OnDirectionChanged += Navigation_OnDirectionChanged;

            clsTaskDownloadData.OnCurrentPoseReq = CurrentPoseReqCallback;

            AGVC.OnTaskActionFinishAndSuccess += AGVMoveTaskActionSuccessHandle;
            AGVC.OnTaskActionFinishCauseAbort += CarController_OnTaskActionFinishCauseAbort;
            AGVC.OnTaskActionFinishButNeedToExpandPath += AGVC_OnTaskActionFinishButNeedToExpandPath; ;
            AGVC.OnMoveTaskStart += CarController_OnMoveTaskStart;
            AGVS.OnTaskDownload += AGVSTaskDownloadConfirm;
            AGVS.OnTaskResetReq = AGVSTaskResetReqHandle;
            AGVS.OnTaskDownloadFeekbackDone += ExecuteAGVSTask;
            Navigation.OnTagReach += OnTagReachHandler;
            BarcodeReader.OnTagLeave += OnTagLeaveHandler;


            AGVC.OnCSTReaderActionDone += CSTReader.UpdateCSTIDDataHandler;


        }

        private void CarController_OnSickDataUpdated(object? sender, LocalizationControllerResultMessage0502 e)
        {
            SickData.StateData = e;
        }

        internal async Task<bool> Initialize()
        {
            BuzzerPlayer.BuzzerStop();
            WagoDO.ResetHandshakeSignals();

            if (!WagoDI.GetState(clsDIModule.DI_ITEM.EMO))
            {
                AlarmManager.AddAlarm(AlarmCodes.EMO_Button);
                BuzzerPlayer.BuzzerAlarm();
                return false;
            }

            if (!WagoDI.GetState(clsDIModule.DI_ITEM.Horizon_Motor_Switch))
            {
                AlarmManager.AddAlarm(AlarmCodes.Switch_Type_Error);
                BuzzerPlayer.BuzzerAlarm();
                return false;
            }

            DirectionLighter.CloseAll();
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
        private (int tag, double locx, double locy, double theta) CurrentPoseReqCallback()
        {
            var tag = Navigation.Data.lastVisitedNode.data;
            var x = Navigation.Data.robotPose.pose.position.x;
            var y = Navigation.Data.robotPose.pose.position.y;
            var theta = BarcodeReader.Data.theta;
            return new(tag, x, y, theta);
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

            //bool frontLaserArea3Triggering = !WagoDI.GetState(clsDIModule.DI_ITEM.FrontProtection_Area_Sensor_3) | !WagoDI.GetState(clsDIModule.DI_ITEM.FrontProtection_Area_Sensor_4);
            //bool backLaserArea3Triggering = !WagoDI.GetState(clsDIModule.DI_ITEM.BackProtection_Area_Sensor_3) | !WagoDI.GetState(clsDIModule.DI_ITEM.BackProtection_Area_Sensor_4);
            //if (!frontLaserArea3Triggering | !backLaserArea3Triggering)
            //    AGVC.FarAreaLaserRecoveryHandler(sender, e);

        }

        private void WagoDI_OnFarAreaLaserTrigger(object? sender, EventArgs e)
        {
            if (Operation_Mode == OPERATOR_MODE.AUTO && RunningTaskData.Action_Type == ACTION_TYPE.None)
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
            if (AGVC.IsAGVExecutingTask)
            {
                DirectionLighter.LightSwitchByAGVDirection(sender, e);
                Laser.LaserChangeByAGVDirection(sender, e);
            }
        }

        private void WagoDI_OnEMO(object? sender, EventArgs e)
        {
            SoftwareEMO();
        }




        internal async Task<bool> CancelInitialize()
        {
            return true;
        }

        internal void SoftwareEMO()
        {
            IsInitialized = false;
            AGVC.EMOHandler("SoftwareEMO", EventArgs.Empty);
            AGVSRemoteModeChangeReq(REMOTE_MODE.OFFLINE);
            Task.Factory.StartNew(async () =>
            {
                Sub_Status = SUB_STATUS.DOWN;
                await Task.Delay(100);
                Sub_Status = SUB_STATUS.ALARM;
                AlarmManager.AddAlarm(AlarmCodes.SoftwareEMS);
            });

        }

        internal async Task ResetAlarmsAsync()
        {
            BuzzerPlayer.BuzzerStop();

            if (AlarmManager.CurrentAlarms.Count == 0)
                return;
            _ = Task.Factory.StartNew(async () =>
             {
                 if (WheelDrivers.Any(dr => dr.State != STATE.NORMAL) | WagoDI.GetState(clsDIModule.DI_ITEM.Horizon_Motor_Error_1) | WagoDI.GetState(clsDIModule.DI_ITEM.Horizon_Motor_Error_2))
                     await WagoDO.ResetMotor();
                 AGVC.CarSpeedControl(CarController.ROBOT_CONTROL_CMD.SPEED_Reconvery);
                 FeedbackTaskStatus(TASK_RUN_STATUS.ACTION_FINISH);

                 if (!AlarmManager.CurrentAlarms.Values.Any(alarm => alarm.ELevel == clsAlarmCode.LEVEL.Alarm))
                 {
                     if (CurrentTaskRunStatus == TASK_RUN_STATUS.NAVIGATING)
                     {
                         Sub_Status = SUB_STATUS.RUN;
                     }
                     else
                         Sub_Status = SUB_STATUS.IDLE;
                 }
                 AlarmManager.ClearAlarm();
             });

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

        /// <summary>
        /// 當要求取得RunningStates Data的callback function
        /// </summary>
        /// <param name="getLastPtPoseOfTrajectory"></param>
        /// <returns></returns>
        internal RunningStatus GenRunningStateReportData(bool getLastPtPoseOfTrajectory = false)
        {
            RunningStatus.clsCorrdination clsCorrdination = new RunningStatus.clsCorrdination();
            MAIN_STATUS _Main_Status = Main_Status;
            if (getLastPtPoseOfTrajectory)
            {
                var lastPt = RunningTaskData.ExecutingTrajecory.Last();
                clsCorrdination.X = lastPt.X;
                clsCorrdination.Y = lastPt.Y;
                clsCorrdination.Theta = lastPt.Theta;
                _Main_Status = MAIN_STATUS.IDLE;
            }
            else
            {
                clsCorrdination.X = Math.Round(Navigation.Data.robotPose.pose.position.x, 3);
                clsCorrdination.Y = Math.Round(Navigation.Data.robotPose.pose.position.y, 3);
                clsCorrdination.Theta = Math.Round(BarcodeReader.Data.theta, 3);
            }
            //gen alarm codes 

            RunningStatus.clsAlarmCode[] alarm_codes = AlarmManager.CurrentAlarms.Select(alarm => new RunningStatus.clsAlarmCode
            {
                Alarm_ID = alarm.Value.Code,
                Alarm_Level = (int)alarm.Value.ELevel,
                Alarm_Description = alarm.Value.Description,
                Alarm_Category = alarm.Value.ELevel == clsAlarmCode.LEVEL.Warning ? 0 : (int)alarm.Value.ELevel

            }).ToArray();

            try
            {
                double[] batteryLevels = Batteries.Select(battery => (double)battery.Value.Data.batteryLevel).ToArray();
                return new RunningStatus
                {
                    Cargo_Status = (!WagoDI.GetState(clsDIModule.DI_ITEM.Cst_Sensor_1) | !WagoDI.GetState(clsDIModule.DI_ITEM.Cst_Sensor_1)) ? 1 : 0,
                    AGV_Status = _Main_Status,
                    Electric_Volume = batteryLevels,
                    Last_Visited_Node = Navigation.Data.lastVisitedNode.data,
                    Corrdination = clsCorrdination,
                    CSTID = new string[] { CSTReader.ValidCSTID },
                    Odometry = Odometry,
                    AGV_Reset_Flag = AGV_Reset_Flag,
                    Alarm_Code = alarm_codes
                };
            }
            catch (Exception ex)
            {
                //LOG.ERROR("GenRunningStateReportData ", ex);
                return new RunningStatus();
            }
        }



        private void CarController_OnModuleInformationUpdated(object? sender, ModuleInformation _ModuleInformation)
        {
            Odometry = _ModuleInformation.Mileage;
            Navigation.StateData = _ModuleInformation.nav_state;

            ushort battery_id = _ModuleInformation.Battery.batteryID;
            if (Batteries.TryGetValue(battery_id, out var battery))
            {
                battery.StateData = _ModuleInformation.Battery;
            }
            else
            {
                Batteries.Add(battery_id, new clsBattery()
                {
                    StateData = _ModuleInformation.Battery
                });
            }

            IMU.StateData = _ModuleInformation.IMU;
            GuideSensor.StateData = _ModuleInformation.GuideSensor;
            BarcodeReader.StateData = _ModuleInformation.reader;
            CSTReader.StateData = _ModuleInformation.CSTReader;
            for (int i = 0; i < _ModuleInformation.Wheel_Driver.driversState.Length; i++)
                WheelDrivers[i].StateData = _ModuleInformation.Wheel_Driver.driversState[i];

            //Task.Factory.StartNew(async() =>
            //{
            //    await Task.Delay(1000);

            //    foreach (var item in CarComponents.Select(comp => comp.ErrorCodes).ToList())
            //    {
            //        foreach (var alarm in item.Keys)
            //        {
            //            AlarmManager.AddWarning(alarm);
            //        }
            //    }

            //});
            if (Batteries.Values.Any(battery => battery.IsCharging))
            {
                if (Batteries.Values.All(battery => battery.Data.batteryLevel >= 99))
                    WagoDO.SetState(clsDOModule.DO_ITEM.Recharge_Circuit, false);//充滿電切斷充電迴路
                Sub_Status = SUB_STATUS.Charging;
            }
            else
            {
                //Task.Factory.StartNew(async () =>
                //{
                //    await Task.Delay(3000);
                //    if (IsInitialized)
                //    {

                //        if (CarController.IsAGVExecutingTask)
                //        {
                //            Sub_Status = SUB_STATUS.RUN;
                //        }
                //        else
                //        {
                //            Sub_Status = SUB_STATUS.IDLE;
                //        }
                //    }
                //    else
                //    {
                //        Sub_Status = SUB_STATUS.DOWN;
                //    }
                //});

            }

        }

        private void WagoDI_OnResetButtonPressed(object? sender, EventArgs e)
        {
            Console.WriteLine("Try Reset Alarms");
        }
        /// <summary>
        /// Auto/Manual 模式切換
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        internal async Task<bool> Auto_Mode_Siwtch(OPERATOR_MODE mode)
        {
            Operation_Mode = mode;
            if (mode == OPERATOR_MODE.AUTO)
            {
                Laser.AllLaserActive();
            }
            else
            {
                Laser.AllLaserDisable();
            }
            return true;
        }
        private bool OnlineModeChangingFlag = false;
        internal async Task<(bool success, RETURN_CODE return_code)> Online_Mode_Switch(REMOTE_MODE mode)
        {
            (bool success, RETURN_CODE return_code) result = await AGVS.TrySendOnlineModeChangeRequest(BarcodeReader.CurrentTag, mode);
            if (!result.success)
                LOG.ERROR($"車輛上線失敗 : Return Code : {result.return_code}");
            else
                Remote_Mode = mode;
            return result;
        }

        internal bool HasAnyCargoOnAGV()
        {
            return !WagoDI.GetState(clsDIModule.DI_ITEM.Cst_Sensor_1) | !WagoDI.GetState(clsDIModule.DI_ITEM.Cst_Sensor_2);
        }


        /// <summary>
        /// 移除卡夾 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        internal async Task<RETURN_CODE> RemoveCstData()
        {
            //向AGVS請求移除卡匣
            string currentCSTID = CSTReader.Data.data;
            string toRemoveCSTID = currentCSTID.ToLower() == "error" ? "" : currentCSTID;

            var retCode = await AGVS.TryRemoveCSTData(toRemoveCSTID, RunningTaskData.Task_Name);
            //清帳
            if (retCode == RETURN_CODE.OK)
                CSTReader.ValidCSTID = "";

            return retCode;
        }


    }
}
