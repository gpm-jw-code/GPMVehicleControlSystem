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
            WagoDI.OnLaserDIRecovery += AGVC.LaserRecoveryHandler;
            WagoDI.OnFarLaserDITrigger += AGVC.FarLaserTriggerHandler;
            WagoDI.OnNearLaserDiTrigger += AGVC.NearLaserTriggerHandler;


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

            _ = Online_Mode_Switch(REMOTE_MODE.OFFLINE);
        }

        private void NearLaserTriggerHandler(object? sender, EventArgs e)
        {
            if (Operation_Mode == OPERATOR_MODE.AUTO && AGVC.IsAGVExecutingTask)
            {
                Sub_Status = SUB_STATUS.ALARM;

                clsIOSignal LaserSignal = sender as clsIOSignal;
                DI_ITEM LaserType = LaserSignal.DI_item;

                AlarmCodes alarm_code = AlarmCodes.None;
                if (LaserType == DI_ITEM.RightProtection_Area_Sensor_2)
                    alarm_code = AlarmCodes.RightProtection_Area3;
                if (LaserType == DI_ITEM.LeftProtection_Area_Sensor_2)
                    alarm_code = AlarmCodes.LeftProtection_Area3;

                if (LaserType == DI_ITEM.FrontProtection_Area_Sensor_2 | LaserType == DI_ITEM.FrontProtection_Area_Sensor_3)
                    alarm_code = AlarmCodes.FrontProtection_Area3;

                if (LaserType == DI_ITEM.BackProtection_Area_Sensor_2 | LaserType == DI_ITEM.BackProtection_Area_Sensor_3)
                    alarm_code = AlarmCodes.BackProtection_Area3;

                if (alarm_code != AlarmCodes.None)
                    AlarmManager.AddAlarm(alarm_code, true);
                else
                {
                    LOG.WARN("Near Laser Trigger but NO Alarm Added!");
                }
            }
        }

        private void FarLaserTriggerHandler(object? sender, EventArgs e)
        {
        }

        private void LaserRecoveryHandler(object? sender, ROBOT_CONTROL_CMD cmd)
        {
            if (Operation_Mode != OPERATOR_MODE.AUTO)
                return;
            if (!AGVC.IsAGVExecutingTask)
                return;

            AlarmManager.ClearAlarm(AlarmCodes.RightProtection_Area3);
            AlarmManager.ClearAlarm(AlarmCodes.LeftProtection_Area3);
            AlarmManager.ClearAlarm(AlarmCodes.FrontProtection_Area3);
            AlarmManager.ClearAlarm(AlarmCodes.BackProtection_Area3);
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

            AlarmManager.AddAlarm(AlarmCodes.AGVs_Abort_Task, false);
            AGV_Reset_Flag = true;
            Task.Factory.StartNew(async () =>
            {
                AGVC.AbortTask(RESET_MODE.ABORT);
                await FeedbackTaskStatus(TASK_RUN_STATUS.ACTION_FINISH);
            });
            Sub_Status = SUB_STATUS.ALARM;
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
            Sub_Status = SUB_STATUS.ALARM;
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

        }


        private void CarController_OnSickDataUpdated(object? sender, LocalizationControllerResultMessage0502 e)
        {
            SickData.StateData = e;
        }
    }
}
