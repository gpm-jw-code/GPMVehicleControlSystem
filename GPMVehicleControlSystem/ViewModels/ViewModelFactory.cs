using static GPMVehicleControlSystem.ViewModels.ForkTestVM;
using GPMVehicleControlSystem.Models.VehicleControl;
using GPMVehicleControlSystem.Models;
using AGVSystemCommonNet6.GPMRosMessageNet.Messages;
using AGVSystemCommonNet6.Alarm.VMS_ALARM;
using AGVSystemCommonNet6.Abstracts;
using static GPMVehicleControlSystem.VehicleControl.DIOModule.clsDOModule;
using AGVSystemCommonNet6;

namespace GPMVehicleControlSystem.ViewModels
{
    public class ViewModelFactory
    {
        internal static Vehicle AgvEntity => StaStored.CurrentVechicle;

        internal static AGVCStatusVM GetVMSStatesVM()
        {

            try
            {
                List<DriverState> driverStates = new List<DriverState>();
                driverStates.AddRange(AgvEntity.WheelDrivers.Select(d => d.Data).ToArray());

                AGVCStatusVM data_view_model = new AGVCStatusVM()
                {
                    APPVersion = StaStored.APPVersion,
                    Agv_Type = AgvEntity.AgvType,
                    Simulation = AgvEntity.SimulationMode,
                    AutoMode = AgvEntity.Operation_Mode,
                    OnlineMode = AgvEntity.Remote_Mode,
                    IsInitialized = AgvEntity.IsInitialized,
                    IsSystemIniting = !AgvEntity.IsSystemInitialized,
                    AGVC_ID = AgvEntity.SID,
                    CarName = AgvEntity.CarName,
                    MainState = AgvEntity.Main_Status.ToString(),
                    SubState = AgvEntity.Sub_Status.ToString(),
                    Tag = AgvEntity.BarcodeReader.CurrentTag,
                    Last_Visit_MapPoint = AgvEntity.lastVisitedMapPoint,
                    Last_Visited_Tag = AgvEntity.Navigation.LastVisitedTag,
                    CST_Data = AgvEntity.CSTReader.ValidCSTID,
                    BatteryStatus = AgvEntity.Batteries.Count == 0 ? new BatteryStateVM()
                    {
                        BatteryLevel = 66
                    } : new BatteryStateVM
                    {
                        BatteryLevel = AgvEntity.Batteries.Values.First().Data.batteryLevel,
                        ChargeCurrent = AgvEntity.Batteries.Values.First().Data.chargeCurrent,
                        IsCharging = AgvEntity.Batteries.Values.First().Data.chargeCurrent != 0,
                        IsError = AgvEntity.Batteries.Values.First().State == CarComponent.STATE.ABNORMAL,
                        CircuitOpened = AgvEntity.WagoDO.GetState(DO_ITEM.Recharge_Circuit)

                    },
                    Pose = AgvEntity.Navigation.Data.robotPose.pose,
                    Angle = AgvEntity.SickData.HeadingAngle,
                    Mileage = AgvEntity.Odometry,
                    BCR_State_MoveBase = AgvEntity.BarcodeReader.Data,
                    AlarmCodes = AlarmManager.CurrentAlarms.Values.ToArray(),
                    MapComparsionRate = AgvEntity.SickData.MapSocre,
                    LocStatus = AgvEntity.SickData.Data.loc_status,
                    AGV_Direct = AgvEntity.Navigation.Direction.ToString().ToUpper(),
                    LinearSpeed = AgvEntity.Navigation.LinearSpeed,
                    AngularSpeed = AgvEntity.Navigation.AngularSpeed,
                    DriversStates = driverStates.ToArray(),
                    Laser_Mode = (int)AgvEntity.Laser.Mode,
                    NavInfo = new NavStateVM
                    {
                        Destination = AgvEntity.ExecutingTask == null ? "" : AgvEntity.ExecutingTask.RunningTaskData.Destination + "",
                        DestinationMapPoint = AgvEntity.DestinationMapPoint,
                        Speed_max_limit = AgvEntity.AGVC.CurrentSpeedLimit,
                        PathPlan = AgvEntity.ExecutingTask == null ? new int[0] : AgvEntity.ExecutingTask.RunningTaskData.ExecutingTrajecory.GetRemainPath(AgvEntity.Navigation.LastVisitedTag)
                    },
                    Current_LASER_MODE = AgvEntity.Laser.Mode.ToString(),
                   
                };
                return data_view_model;
            }
            catch (Exception ex)
            {
                return new AGVCStatusVM();
            }

        }


        internal static ConnectionStateVM GetConnectionStatesVM()
        {
            if (AgvEntity.SimulationMode)
            {
                return new ConnectionStateVM
                {
                    AGVC = ConnectionStateVM.CONNECTION.CONNECTED,
                    RosbridgeServer = ConnectionStateVM.CONNECTION.CONNECTED,
                    WAGO = ConnectionStateVM.CONNECTION.CONNECTED,
                };
            }

            ConnectionStateVM data_view_model = new ConnectionStateVM()
            {
                RosbridgeServer = AgvEntity.AGVC.IsConnected() ? ConnectionStateVM.CONNECTION.CONNECTED : ConnectionStateVM.CONNECTION.DISCONNECT,
                VMS = AgvEntity.AGVS.IsConnected() ? ConnectionStateVM.CONNECTION.CONNECTED : ConnectionStateVM.CONNECTION.DISCONNECT,
                WAGO = AgvEntity.WagoDI.IsConnected() ? ConnectionStateVM.CONNECTION.CONNECTED : ConnectionStateVM.CONNECTION.DISCONNECT,
            };
            return data_view_model;
        }

        internal static DIOTableVM GetDIOTableVM()
        {
            return new DIOTableVM
            {
                Inputs = AgvEntity.WagoDI.VCSInputs.ToList(),
                Outputs = AgvEntity.WagoDO.VCSOutputs.ToList(),
                IsE84HsUseEmulator = AgvEntity.EQ_HS_Method == Vehicle.EQ_HS_METHOD.EMULATION
            };
        }

    }
}
