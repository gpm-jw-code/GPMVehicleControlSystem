using AGVSystemCommonNet6.AGVDispatch.Messages;
using AGVSystemCommonNet6.AGVDispatch;
using AGVSystemCommonNet6.Alarm.VMS_ALARM;
using GPMVehicleControlSystem.Models.VehicleControl.VehicleComponent;
using static AGVSystemCommonNet6.clsEnums;
using AGVSystemCommonNet6.GPMRosMessageNet.Messages;
using GPMVehicleControlSystem.VehicleControl.DIOModule;
using AGVSystemCommonNet6.GPMRosMessageNet.Messages.SickMsg;

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

            //WagoDI.OnFrontArea1LaserTrigger += WagoDI_OnFarAreaLaserTrigger;
            //WagoDI.OnBackArea1LaserTrigger += WagoDI_OnFarAreaLaserTrigger;
            //WagoDI.OnFrontArea1LaserRecovery += WagoDI_OnFarAreaLaserRecovery;
            //WagoDI.OnBackArea1LaserRecovery += WagoDI_OnFarAreaLaserRecovery;
            //WagoDI.OnFrontArea2LaserRecovery += WagoDI_OnFarAreaLaserRecovery;
            //WagoDI.OnBackArea2LaserRecovery += WagoDI_OnFarAreaLaserRecovery;



            WagoDI.OnFrontArea2LaserTrigger += WagoDI_OnFronNearAreaLaserTrigger;
            WagoDI.OnBackArea2LaserTrigger += WagoDI_OnBackNearAreaLaserTrigger;

            WagoDI.OnFrontNearAreaLaserTrigger += WagoDI_OnFronNearAreaLaserTrigger;
            WagoDI.OnBackNearAreaLaserTrigger += WagoDI_OnBackNearAreaLaserTrigger;

            WagoDI.OnFrontArea2LaserRecovery += WagoDI_OnFrontNearAreaLaserRecovery;
            WagoDI.OnBackArea2LaserRecovery += WagoDI_OnBackNearAreaLaserRecovery;


            WagoDI.OnFrontNearAreaLaserRecovery += WagoDI_OnFrontNearAreaLaserRecovery;
            WagoDI.OnBackNearAreaLaserRecovery += WagoDI_OnBackNearAreaLaserRecovery;

            Navigation.OnDirectionChanged += Navigation_OnDirectionChanged;

            clsTaskDownloadData.OnCurrentPoseReq = CurrentPoseReqCallback;


            AGVS.OnTaskDownload += AGVSTaskDownloadConfirm;
            AGVS.OnTaskResetReq = AGVSTaskResetReqHandle;
            AGVS.OnTaskDownloadFeekbackDone += ExecuteAGVSTask;
            Navigation.OnTagReach += OnTagReachHandler;
            BarcodeReader.OnTagLeave += OnTagLeaveHandler;


            AGVC.OnCSTReaderActionDone += CSTReader.UpdateCSTIDDataHandler;


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
            AlarmManager.AddAlarm(AlarmCodes.AGVs_Abort_Task);
            AGV_Reset_Flag = true;
            Task.Factory.StartNew(async () =>
            {
                AGVC.AbortTask(mode);
                await FeedbackTaskStatus(TASK_RUN_STATUS.ACTION_FINISH);
            });
            Sub_Status = SUB_STATUS.ALARM;
            ExecutingTask.Abort();
            return true;
        }
        private void WagoDI_OnFarAreaLaserTrigger(object? sender, EventArgs e)
        {
            //if (Operation_Mode == OPERATOR_MODE.AUTO && ExecutingTask?.action == ACTION_TYPE.None)
            //    Sub_Status = SUB_STATUS.WARNING;
        }


        private void WagoDI_OnFrontNearAreaLaserRecovery(object? sender, EventArgs e)
        {
            if (Operation_Mode != OPERATOR_MODE.AUTO)
                return;

            if (Main_Status == MAIN_STATUS.RUN)
                Sub_Status = SUB_STATUS.RUN;
            else if (Main_Status == MAIN_STATUS.IDLE)
                Sub_Status = SUB_STATUS.IDLE;
            else if (Main_Status == MAIN_STATUS.DOWN)
                Sub_Status = SUB_STATUS.DOWN;

            AlarmManager.ClearAlarm(AlarmCodes.FrontProtection_Area3);
        }

        private void WagoDI_OnBackNearAreaLaserRecovery(object? sender, EventArgs e)
        {
            if (Operation_Mode != OPERATOR_MODE.AUTO)
                return;

            if (Main_Status == MAIN_STATUS.RUN)
                Sub_Status = SUB_STATUS.RUN;
            else if (Main_Status == MAIN_STATUS.IDLE)
                Sub_Status = SUB_STATUS.IDLE;
            else if (Main_Status == MAIN_STATUS.DOWN)
                Sub_Status = SUB_STATUS.DOWN;

            AlarmManager.ClearAlarm(AlarmCodes.BackProtection_Area3);
        }
        private void WagoDI_OnFronNearAreaLaserTrigger(object? sender, EventArgs e)
        {
            if (Operation_Mode == OPERATOR_MODE.AUTO && Sub_Status == SUB_STATUS.RUN)
            {
                Sub_Status = SUB_STATUS.ALARM;
                AlarmManager.AddAlarm(AlarmCodes.FrontProtection_Area3);
            }
        }

        private void WagoDI_OnBackNearAreaLaserTrigger(object? sender, EventArgs e)
        {
            if (Operation_Mode == OPERATOR_MODE.AUTO && Sub_Status == SUB_STATUS.RUN)
            {
                Sub_Status = SUB_STATUS.ALARM;
                AlarmManager.AddAlarm(AlarmCodes.BackProtection_Area3);
            }
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
            SoftwareEMO();
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


        private void CarController_OnSickDataUpdated(object? sender, LocalizationControllerResultMessage0502 e)
        {
            SickData.StateData = e;
        }
    }
}
