using static GPMVehicleControlSystem.ViewModels.ForkTestVM;
using GPMRosMessageNet.Messages;
using GPMVehicleControlSystem.Models.VehicleControl;
using GPMVehicleControlSystem.Models;
using static GPMVehicleControlSystem.Models.VehicleControl.DIOModule.clsDOModule;

namespace GPMVehicleControlSystem.ViewModels
{
    public class ViewModelFactory
    {
        internal static Vehicle AgvEntity => StaStored.CurrentVechicle;

        internal static AGVCStatusVM GetVMSStatesVM()
        {

            List<DriverState> driverStates = new List<DriverState>();
            driverStates.AddRange(AgvEntity.WheelDrivers.Select(d => d.Data).ToArray());

            try
            {

                AGVCStatusVM data_view_model = new AGVCStatusVM()
                {
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
                    CST_Data = AgvEntity.CSTReader.Data.data,
                    BatteryStatus = new BatteryStateVM
                    {
                        BatteryLevel = AgvEntity.Battery.Data.batteryLevel,
                        ChargeCurrent = AgvEntity.Battery.Data.chargeCurrent,
                        IsCharging = AgvEntity.Battery.Data.chargeCurrent != 0,
                        IsError = AgvEntity.Battery.State == Models.VehicleControl.VehicleComponent.Abstracts.CarComponent.STATE.ABNORMAL
                    },
                    Pose = AgvEntity.Navigation.Data.robotPose.pose,
                    Mileage = AgvEntity.Odometry,
                    BCR_State_MoveBase = AgvEntity.BarcodeReader.Data,
                    //AlarmCodes = AlarmManager.CurrentAlarms.Values.ToArray(),
                    //MapComparsionRate = AgvEntity.SickData.MapScore,
                    //ZAxisDriverState = AgvEntity.ModuleInformation.Action_Driver,
                    //ZAxisActionName = AgvEntity.currentVerticalAction,
                    //AGV_Direct = AgvEntity.CurrentDirect.ToString().ToUpper(),
                    DriversStates = driverStates.ToArray(),
                    Laser_Mode = (int)AgvEntity.Laser.Mode,
                    //UltrSensorState = AgvEntity.ModuleInformation.UltrasonicSensor,
                    //IsAGVPoseError = ((clsAGVPoseMonitor)ComponentMonitorManager.agvPoseMonitor).IsPoseError,
                    NavInfo = new NavStateVM
                    {
                        Destination = AgvEntity.CarController.RunningTaskData.Destination + "",
                        Speed_max_limit = AgvEntity.CarController.CurrentSpeedLimit
                    },
                    Current_LASER_MODE = AgvEntity.Laser.Mode.ToString(),
                    LightsStates = new AGV_VMS.ViewModels.LightsStatesVM
                    {
                        Front = AgvEntity.WagoDO.GetState(DO_ITEM.AGV_DiractionLight_Front),
                        Back = AgvEntity.WagoDO.GetState(DO_ITEM.AGV_DiractionLight_Back),
                        Right = AgvEntity.WagoDO.GetState(DO_ITEM.AGV_DiractionLight_Right),
                        Left = AgvEntity.WagoDO.GetState(DO_ITEM.AGV_DiractionLight_Left),
                        Run = AgvEntity.WagoDO.GetState(DO_ITEM.AGV_DiractionLight_G),
                        Down = AgvEntity.WagoDO.GetState(DO_ITEM.AGV_DiractionLight_R),
                        Idle = AgvEntity.WagoDO.GetState(DO_ITEM.AGV_DiractionLight_Y),
                        Online = AgvEntity.WagoDO.GetState(DO_ITEM.AGV_DiractionLight_B),
                    }
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
                RosbridgeServer = AgvEntity.CarController.IsConnected() ? ConnectionStateVM.CONNECTION.CONNECTED : ConnectionStateVM.CONNECTION.DISCONNECT,
                VMS = AgvEntity.AGVSConnection.IsConnected() ? ConnectionStateVM.CONNECTION.CONNECTED : ConnectionStateVM.CONNECTION.DISCONNECT,
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
            };
        }

    }
}
