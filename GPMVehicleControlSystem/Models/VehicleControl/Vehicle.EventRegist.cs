using AGVSystemCommonNet6.AGVDispatch.Messages;
using AGVSystemCommonNet6.AGVDispatch;
using AGVSystemCommonNet6.Alarm.VMS_ALARM;
using GPMVehicleControlSystem.Models.VehicleControl.VehicleComponent;
using static AGVSystemCommonNet6.clsEnums;
using AGVSystemCommonNet6.GPMRosMessageNet.Messages;
using GPMVehicleControlSystem.VehicleControl.DIOModule;
using AGVSystemCommonNet6.GPMRosMessageNet.Messages.SickMsg;
using static GPMVehicleControlSystem.VehicleControl.DIOModule.clsDIModule;
using static GPMVehicleControlSystem.Models.VehicleControl.AGVControl.CarController;
using AGVSystemCommonNet6.Log;

namespace GPMVehicleControlSystem.Models.VehicleControl
{
    public partial class Vehicle
    {
        private void EventsRegist() //TODO EventRegist
        {
            AGVSMessageFactory.OnVCSRunningDataRequest += GenRunningStateReportData;
            AGVS.OnRemoteModeChanged = AGVSRemoteModeChangeReq;
            AGVC.OnModuleInformationUpdated += CarController_OnModuleInformationUpdated;
            AGVC.OnSickDataUpdated += CarController_OnSickDataUpdated;
            WagoDI.OnEMO += WagoDI_OnEMO;
            WagoDI.OnBumpSensorPressed += WagoDI_OnBumpSensorPressed;
            WagoDI.OnEMO += AGVC.EMOHandler;
            WagoDI.OnResetButtonPressed += async (s, e) => await ResetAlarmsAsync(true);

            WagoDI.OnLaserDIRecovery += LaserRecoveryHandler;
            WagoDI.OnFarLaserDITrigger += FarLaserTriggerHandler;
            WagoDI.OnNearLaserDiTrigger += NearLaserTriggerHandler;

            Navigation.OnDirectionChanged += Navigation_OnDirectionChanged;

            clsTaskDownloadData.OnCurrentPoseReq = CurrentPoseReqCallback;


            AGVS.OnTaskDownload += AGVSTaskDownloadConfirm;
            AGVS.OnTaskResetReq = AGVSTaskResetReqHandle;
            AGVS.OnTaskDownloadFeekbackDone += ExecuteAGVSTask;
            Navigation.OnTagReach += OnTagReachHandler;
            BarcodeReader.OnTagLeave += OnTagLeaveHandler;

            AGVC.OnCSTReaderActionDone += CSTReader.UpdateCSTIDDataHandler;

            AlarmManager.OnUnRecoverableAlarmOccur += AlarmManager_OnUnRecoverableAlarmOccur;

        }


        private async void AlarmManager_OnUnRecoverableAlarmOccur(object? sender, EventArgs e)
        {
            _ = Task.Factory.StartNew(async () =>
            {
                if (Remote_Mode == REMOTE_MODE.ONLINE)
                    await Online_Mode_Switch(REMOTE_MODE.OFFLINE);
            });
        }

        private void NearLaserTriggerHandler(object? sender, EventArgs e)
        {

            if (Operation_Mode == OPERATOR_MODE.AUTO && AGVC.IsAGVExecutingTask)
            {
                AGVC.NearLaserTriggerHandler(sender, e);

                Sub_Status = SUB_STATUS.ALARM;
                clsIOSignal LaserSignal = sender as clsIOSignal;
                DI_ITEM LaserType = LaserSignal.DI_item;

                AlarmCodes alarm_code = GetAlarmCodeByLsrDI(LaserType);

                if (alarm_code != AlarmCodes.None)
                    AlarmManager.AddAlarm(alarm_code, true);
                else
                {
                    LOG.WARN("Near Laser Trigger but NO Alarm Added!");
                }
            }
        }

        private static AlarmCodes GetAlarmCodeByLsrDI(DI_ITEM LaserType)
        {
            AlarmCodes alarm_code = AlarmCodes.None;
            if (LaserType == DI_ITEM.RightProtection_Area_Sensor_2)
                alarm_code = AlarmCodes.RightProtection_Area3;
            if (LaserType == DI_ITEM.LeftProtection_Area_Sensor_2)
                alarm_code = AlarmCodes.LeftProtection_Area3;

            if (LaserType == DI_ITEM.FrontProtection_Area_Sensor_2 | LaserType == DI_ITEM.FrontProtection_Area_Sensor_3)
                alarm_code = AlarmCodes.FrontProtection_Area3;

            if (LaserType == DI_ITEM.BackProtection_Area_Sensor_2 | LaserType == DI_ITEM.BackProtection_Area_Sensor_3)
                alarm_code = AlarmCodes.BackProtection_Area3;
            return alarm_code;
        }

        private void FarLaserTriggerHandler(object? sender, EventArgs e)
        {
            AGVC.FarLaserTriggerHandler(sender, e);

        }

        private void LaserRecoveryHandler(object? sender, ROBOT_CONTROL_CMD cmd)
        {

            AGVC.LaserRecoveryHandler(sender, cmd);
            if ((cmd == ROBOT_CONTROL_CMD.NONE))
                return;
            clsIOSignal LaserSignal = sender as clsIOSignal;
            DI_ITEM LaserType = LaserSignal.DI_item;

            AlarmCodes alarm_code = GetAlarmCodeByLsrDI(LaserType);
            if (alarm_code != AlarmCodes.None)
                AlarmManager.ClearAlarm(alarm_code);

            if (Operation_Mode != OPERATOR_MODE.AUTO)
                return;
            if (!AGVC.IsAGVExecutingTask)
                return;
            if (LaserType != DI_ITEM.FrontProtection_Area_Sensor_3 && LaserType != DI_ITEM.BackProtection_Area_Sensor_3)
                Sub_Status = SUB_STATUS.RUN;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="taskDownloadData"></param>
        /// <returns></returns>
        internal bool AGVSTaskDownloadConfirm(clsTaskDownloadData taskDownloadData)
        {
            AGV_Reset_Flag = false;

            if (Main_Status == MAIN_STATUS.DOWN) //TODO More Status Confirm when recieve AGVS Task
                return false;

            return true;
        }

        internal bool AGVSTaskResetReqHandle(RESET_MODE mode)
        {
            if (!AGVC.IsAGVExecutingTask)
                return true;
            AGV_Reset_Flag = true;
            Task.Factory.StartNew(async () =>
            {
                AGVC.AbortTask(RESET_MODE.CYCLE_STOP);
                await Task.Delay(500);
                await FeedbackTaskStatus(TASK_RUN_STATUS.ACTION_FINISH);
            });
            Sub_Status = SUB_STATUS.IDLE;
            ExecutingTask.Abort();
            return true;
        }

        private void Navigation_OnDirectionChanged(object? sender, clsNavigation.AGV_DIRECTION direction)
        {
            if (AGVC.IsAGVExecutingTask && ExecutingTask.action == ACTION_TYPE.None)
            {

                DirectionLighter.LightSwitchByAGVDirection(sender, direction);

                if (ExecutingTask.action == ACTION_TYPE.None && direction != clsNavigation.AGV_DIRECTION.STOP)
                    Laser.LaserChangeByAGVDirection(sender, direction);
            }
        }

        private void WagoDI_OnEMO(object? sender, EventArgs e)
        {
            IsInitialized = false;
            ExecutingTask?.Abort();
            AGVSRemoteModeChangeReq(REMOTE_MODE.OFFLINE);
            Sub_Status = SUB_STATUS.ALARM;
        }

        private void WagoDI_OnBumpSensorPressed(object? sender, EventArgs e)
        {
            IsInitialized = false;
            ExecutingTask?.Abort();
            AlarmManager.AddAlarm(AlarmCodes.Bumper, false);
            Sub_Status = SUB_STATUS.DOWN;
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


            var _lastVisitedMapPoint = NavingMap.Points.Values.FirstOrDefault(pt => pt.TagNumber == this.Navigation.LastVisitedTag);
            lastVisitedMapPoint = _lastVisitedMapPoint == null ? new AGVSystemCommonNet6.MAP.MapPoint() { Name = "Unknown" } : _lastVisitedMapPoint;
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
                Sub_Status = SUB_STATUS.Charging;
            }
            else
            {

            }

        }

        private void WagoDI_OnResetButtonPressed(object? sender, EventArgs e)
        {

        }


        private void CarController_OnSickDataUpdated(object? sender, LocalizationControllerResultMessage0502 e)
        {
            SickData.StateData = e;
        }
    }
}
