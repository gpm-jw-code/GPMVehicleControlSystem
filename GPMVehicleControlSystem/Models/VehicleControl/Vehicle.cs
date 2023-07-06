using AGVSystemCommonNet6.Abstracts;
using AGVSystemCommonNet6.AGVDispatch;
using AGVSystemCommonNet6.AGVDispatch.Messages;
using AGVSystemCommonNet6.AGVDispatch.Model;
using AGVSystemCommonNet6.Alarm.VMS_ALARM;
using AGVSystemCommonNet6.GPMRosMessageNet.Messages;
using AGVSystemCommonNet6.GPMRosMessageNet.Messages.SickMsg;
using AGVSystemCommonNet6.Log;
using AGVSystemCommonNet6.MAP;
using GPMVehicleControlSystem.Models.Buzzer;
using GPMVehicleControlSystem.Models.Emulators;
using GPMVehicleControlSystem.Models.NaviMap;
using GPMVehicleControlSystem.Models.VehicleControl.AGVControl;
using GPMVehicleControlSystem.Models.VehicleControl.VehicleComponent;
using GPMVehicleControlSystem.Tools;
using GPMVehicleControlSystem.VehicleControl.DIOModule;
using System.Diagnostics;
using System.Net.Sockets;
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

        public enum VMS_PROTOCOL
        {
            HTTP,
            TCPIP
        }



        public clsDirectionLighter DirectionLighter { get; set; }
        public clsStatusLighter StatusLighter { get; set; }
        public clsAGVSConnection AGVS;
        public VMS_PROTOCOL VmsProtocol = VMS_PROTOCOL.HTTP;
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
        public Map NavingMap = new Map()
        {
            Name = "No_load",
            Points = new Dictionary<int, MapPoint>()
        };

        /// <summary>
        /// 里程數
        /// </summary>
        public double Odometry;
        VehicleEmu emulator;

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
                if (SimulationMode)
                    return;
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
        public bool AGV_Reset_Flag { get; internal set; }

        public MoveControl ManualController => AGVC.ManualController;

        public AGV_TYPE AgvType { get; internal set; } = AGV_TYPE.SUBMERGED_SHIELD;
        public bool SimulationMode { get; internal set; } = false;
        public bool IsInitialized { get; internal set; }
        public bool IsSystemInitialized { get; internal set; }
        private SUB_STATUS _Sub_Status = SUB_STATUS.DOWN;
        public MapPoint lastVisitedMapPoint { get; private set; } = new MapPoint { Name = "Unkown" };
        public MapPoint DestinationMapPoint
        {
            get
            {
                if (ExecutingTask == null)
                    return new MapPoint { Name = "" };
                else
                {
                    var _point = NavingMap.Points.Values.FirstOrDefault(pt => pt.TagNumber == ExecutingTask.RunningTaskData.Destination);
                    return _point == null ? new MapPoint { Name = "Unknown" } : _point;
                }
            }
        }

        public SUB_STATUS Sub_Status
        {
            get => _Sub_Status;
            set
            {
                if (_Sub_Status != value)
                {
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
                        AGVC.IsAGVExecutingTask = false;
                        BuzzerPlayer.BuzzerStop();
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
                        if (ExecutingTask != null)
                        {
                            Task.Run(async () =>
                            {
                                await BuzzerPlayer.BuzzerStop();
                                await Task.Delay(50);
                                if (ExecutingTask.action == ACTION_TYPE.None)
                                    BuzzerPlayer.BuzzerMoving();
                                else
                                    BuzzerPlayer.BuzzerAction();
                            });
                        }
                    }

                    _Sub_Status = value;
                }
            }
        }

        public Vehicle()
        {
            ReadTaskNameFromFile();
            IsSystemInitialized = false;
            SimulationMode = AppSettingsHelper.GetValue<bool>("VCS:SimulationMode");
            emulator = new VehicleEmu(7);
            string AGVS_IP = AppSettingsHelper.GetValue<string>("VCS:Connections:AGVS:IP");
            int AGVS_Port = AppSettingsHelper.GetValue<int>("VCS:Connections:AGVS:Port");
            string AGVS_LocalIP = AppSettingsHelper.GetValue<string>("VCS:Connections:AGVS:LocalIP");
            VmsProtocol = AppSettingsHelper.GetValue<int>("VCS:Connections:AGVS:Protocol") == 0 ? VMS_PROTOCOL.TCPIP : VMS_PROTOCOL.HTTP;
            string Wago_IP = AppSettingsHelper.GetValue<string>("VCS:Connections:Wago:IP");
            int Wago_Port = AppSettingsHelper.GetValue<int>("VCS:Connections:Wago:Port");

            string RosBridge_IP = AppSettingsHelper.GetValue<string>("VCS:Connections:RosBridge:IP");
            int RosBridge_Port = AppSettingsHelper.GetValue<int>("VCS:Connections:RosBridge:Port");
            SID = AppSettingsHelper.GetValue<string>("VCS:SID");
            CarName = AppSettingsHelper.GetValue<string>("VCS:EQName");

            WagoDO = new clsDOModule(Wago_IP, Wago_Port, null);
            WagoDI = new clsDIModule(Wago_IP, Wago_Port, WagoDO);
            AGVC = new CarController(RosBridge_IP, RosBridge_Port);
            AGVS = new clsAGVSConnection(AGVS_IP, AGVS_Port, AGVS_LocalIP);
            AGVS.UseWebAPI = VmsProtocol == VMS_PROTOCOL.HTTP;

            DirectionLighter = new clsDirectionLighter(WagoDO);
            StatusLighter = new clsStatusLighter(WagoDO);
            Laser = new clsLaser(WagoDO, WagoDI);

            Task RosConnTask = new Task(async () =>
            {
                await Task.Delay(1).ContinueWith(t =>
                AGVC.Connect());
                AGVC.ManualController.vehicle = this;
                BuzzerPlayer.rossocket = AGVC.rosSocket;
                BuzzerPlayer.BuzzerAlarm();
            });

            Task WagoDOConnTask = new Task(() =>
            {
                try
                {
                    WagoDO.Connect();
                    Laser.Mode = LASER_MODE.Bypass;
                    StatusLighter.CloseAll();
                    StatusLighter.DOWN();
                }
                catch (SocketException ex)
                {
                }
                catch (Exception ex)
                {
                }
            });

            Task WagoDIConnTask = new Task(() =>
            {
                try
                {
                    WagoDI.Connect();
                    WagoDI.StartAsync();
                }
                catch (SocketException ex)
                {
                }
                catch (Exception ex)
                {
                }
            });
            EventsRegist();
            if (!SimulationMode)
            {
                RosConnTask.Start();
                WagoDOConnTask.Start();
                WagoDIConnTask.Start();
            }
            AGVSMessageFactory.Setup(SID, CarName);
            //Pilot = new AGVPILOT(this);
            IsSystemInitialized = true;

            Task.Run(async () =>
            {
                try
                {
                    NavingMap = await MapStore.GetMapFromServer();
                }
                catch (Exception ex)
                {
                }
            });

            Task.Factory.StartNew(async () =>
            {
                await Task.Delay(1000);
                AGVS.Start();
            });

        }


        internal async Task<bool> Initialize()
        {
            try
            {
                BuzzerPlayer.BuzzerStop();
                if (SimulationMode)
                {
                    IsInitialized = true;
                    AGVC.IsAGVExecutingTask = false;
                    _Sub_Status = SUB_STATUS.IDLE;
                    emulator.Runstatus.AGV_Status = Main_Status = MAIN_STATUS.IDLE;
                    return true;
                }
                WagoDO.ResetHandshakeSignals();

                if (!WagoDI.GetState(clsDIModule.DI_ITEM.EMO))
                {
                    AlarmManager.AddAlarm(AlarmCodes.EMO_Button, false);
                    BuzzerPlayer.BuzzerAlarm();
                    return false;
                }

                if (!WagoDI.GetState(clsDIModule.DI_ITEM.Horizon_Motor_Switch))
                {
                    AlarmManager.AddAlarm(AlarmCodes.Switch_Type_Error, false);
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
                StatusLighter.INITIALIZE();
                await Task.Delay(1000);
                StatusLighter.AbortFlash();

                IsInitialized = true;
                Sub_Status = SUB_STATUS.IDLE;
                AGVC.IsAGVExecutingTask = false;
                return true;
            }
            catch (SocketException ex)
            {
                AlarmManager.AddAlarm(AlarmCodes.Wago_IO_Write_Fail, false);
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }

        }
        private (int tag, double locx, double locy, double theta) CurrentPoseReqCallback()
        {
            var tag = Navigation.Data.lastVisitedNode.data;
            var x = Navigation.Data.robotPose.pose.position.x;
            var y = Navigation.Data.robotPose.pose.position.y;
            var theta = BarcodeReader.Data.theta;
            return new(tag, x, y, theta);
        }


        internal async Task<bool> CancelInitialize()
        {
            return true;
        }

        internal void SoftwareEMO()
        {
            IsInitialized = false;
            AGVC.EMOHandler("SoftwareEMO", EventArgs.Empty);
            ExecutingTask?.Abort();
            AGVSRemoteModeChangeReq(REMOTE_MODE.OFFLINE);
            Task.Factory.StartNew(async () =>
            {
                Sub_Status = SUB_STATUS.DOWN;
                await Task.Delay(100);
                Sub_Status = SUB_STATUS.ALARM;
                AlarmManager.AddAlarm(AlarmCodes.SoftwareEMS, false);
            });

        }

        internal async Task ResetAlarmsAsync(bool IsTriggerByButton)
        {
            BuzzerPlayer.BuzzerStop();

            _ = Task.Factory.StartNew(async () =>
             {
                 if (AlarmManager.CurrentAlarms.Values.All(alarm => !alarm.IsRecoverable))
                 {
                     FeedbackTaskStatus(TASK_RUN_STATUS.ACTION_FINISH);
                     Sub_Status = SUB_STATUS.STOP;
                 }
                 else
                 {
                     if (CurrentTaskRunStatus == TASK_RUN_STATUS.NAVIGATING)
                     {
                         Sub_Status = SUB_STATUS.RUN;
                     }
                     else
                     {
                         if (Sub_Status != SUB_STATUS.IDLE)
                             Sub_Status = SUB_STATUS.STOP;
                     }
                     AGVC.CarSpeedControl(CarController.ROBOT_CONTROL_CMD.SPEED_Reconvery);
                 }
                 AlarmManager.ClearAlarm();

                 if (!IsTriggerByButton)
                     await WagoDO.ResetMotor();
                 else
                     await Task.Delay(2000);

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
            clsCoordination clsCorrdination = new clsCoordination();
            MAIN_STATUS _Main_Status = Main_Status;
            if (getLastPtPoseOfTrajectory)
            {
                var lastPt = ExecutingTask.RunningTaskData.ExecutingTrajecory.Last();
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

            RunningStatus.clsAlarmCode[] alarm_codes = AlarmManager.CurrentAlarms.ToList().FindAll(alarm => alarm.Value.EAlarmCode != AlarmCodes.None).Select(alarm => new RunningStatus.clsAlarmCode
            {
                Alarm_ID = alarm.Value.Code,
                Alarm_Level = alarm.Value.IsRecoverable ? 0 : 1,
                Alarm_Description = alarm.Value.Description,
                Alarm_Category = alarm.Value.IsRecoverable ? 0 : 1,


            }).ToArray();

            try
            {
                double[] batteryLevels = Batteries.Select(battery => (double)battery.Value.Data.batteryLevel).ToArray();
                return SimulationMode ? emulator.Runstatus : new RunningStatus
                {
                    Cargo_Status = HasAnyCargoOnAGV() ? 1 : 0,
                    AGV_Status = _Main_Status,
                    Electric_Volume = batteryLevels,
                    Last_Visited_Node = Navigation.Data.lastVisitedNode.data,
                    Coordination = clsCorrdination,
                    CSTID = CSTReader.ValidCSTID == "" ? new string[0] : new string[] { CSTReader.ValidCSTID },
                    Odometry = Odometry,
                    AGV_Reset_Flag = AGV_Reset_Flag,
                    Alarm_Code = alarm_codes,
                    Escape_Flag = ExecutingTask == null ? false : ExecutingTask.RunningTaskData.Escape_Flag
                };
            }
            catch (Exception ex)
            {
                //LOG.ERROR("GenRunningStateReportData ", ex);
                return new RunningStatus();
            }
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
            (bool success, RETURN_CODE return_code) result = await AGVS.TrySendOnlineModeChangeRequest(Navigation.LastVisitedTag, mode);
            if (!result.success)
                LOG.ERROR($"車輛上線失敗 : Return Code : {result.return_code}");
            else
                Remote_Mode = mode;
            return result;
        }

        internal bool HasAnyCargoOnAGV()
        {
            return !WagoDI.GetState(clsDIModule.DI_ITEM.Cst_Sensor_1) && !WagoDI.GetState(clsDIModule.DI_ITEM.Cst_Sensor_2);
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
            var retCode = await AGVS.TryRemoveCSTData(toRemoveCSTID, "");
            //清帳
            if (retCode == RETURN_CODE.OK)
                CSTReader.ValidCSTID = "";

            return retCode;
        }


    }
}
